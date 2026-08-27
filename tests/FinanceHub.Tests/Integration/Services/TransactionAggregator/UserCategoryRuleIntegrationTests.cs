using System;
using System.Threading;
using System.Threading.Tasks;
using FinanceHub.TransactionAggregator.Application.Commands.CategorizeTransaction;
using FinanceHub.TransactionAggregator.Application.Commands.IngestTransaction;
using FinanceHub.TransactionAggregator.Application.Services.Categorization;
using FinanceHub.TransactionAggregator.Domain.Entities;
using FinanceHub.TransactionAggregator.Domain.ValueObjects;
using FinanceHub.TransactionAggregator.Infrastructure.Persistence;
using FinanceHub.TransactionAggregator.Infrastructure.Persistence.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FinanceHub.Tests.Integration.Services.TransactionAggregator;

public class UserCategoryRuleIntegrationTests : IDisposable
{
    private readonly TransactionAggregatorDbContext _context;
    private readonly TransactionRepository _txRepo;
    private readonly UserCategoryRuleRepository _ruleRepo;
    private readonly AccountBalanceRepository _balanceRepo;

    private readonly CategorizeTransactionCommandHandler _categorizeHandler;
    private readonly IngestTransactionCommandHandler _ingestHandler;

    public UserCategoryRuleIntegrationTests()
    {
        var options = new DbContextOptionsBuilder<TransactionAggregatorDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new TransactionAggregatorDbContext(options);
        _txRepo = new TransactionRepository(_context);
        _ruleRepo = new UserCategoryRuleRepository(_context);
        _balanceRepo = new AccountBalanceRepository(_context);

        var pipeline = new CategoryResolverPipeline(
            new ICategoryResolver[]
            {
                new UserCustomRuleCategoryResolver(_ruleRepo),
                new DefaultFallbackCategoryResolver()
            });

        var unitOfWork = new UnitOfWork(_context);
        var eventPublisher = NSubstitute.Substitute.For<FinanceHub.TransactionAggregator.Application.Interfaces.IEventPublisher>();

        _categorizeHandler = new CategorizeTransactionCommandHandler(_txRepo, _ruleRepo);
        var categoryRepo = new FinanceHub.TransactionAggregator.Infrastructure.Persistence.Repositories.CategoryRepository(_context);
        _ingestHandler = new IngestTransactionCommandHandler(_txRepo, _balanceRepo, categoryRepo, pipeline, eventPublisher, unitOfWork);
    }

    [Fact]
    public async Task CategorizeTransaction_WithApplyToPastTransactions_ShouldRetrocategorizePastTransactions()
    {
        // Arrange
        const string userId = "test-user-1";
        var petShopCatId = Guid.NewGuid();

        // 1. Ingest a past transaction (born with default fallback category)
        var pastCommand = new IngestTransactionCommand(
            userId, "Inter", "acc-001", "tx-past-1", 45.00m, "BRL",
            TransactionType.Debit, "CUSCO BOX PORTO ALEGRE BRA",
            DateTime.UtcNow.AddDays(-10), TransactionChannel.Other, "CUSCO BOX PORTO ALEGRE BRA");

        var pastTxId = await _ingestHandler.Handle(pastCommand, CancellationToken.None);

        // 2. Ingest a recent transaction
        var recentCommand = new IngestTransactionCommand(
            userId, "Inter", "acc-001", "tx-recent-1", 59.90m, "BRL",
            TransactionType.Debit, "CUSCO BOX PORTO ALEGRE BRA",
            DateTime.UtcNow, TransactionChannel.Other, "CUSCO BOX PORTO ALEGRE BRA");

        var recentTxId = await _ingestHandler.Handle(recentCommand, CancellationToken.None);

        // Act: Categorize the recent transaction to Pet Shop with ApplyToPastTransactions = true
        var categorizeCommand = new CategorizeTransactionCommand(
            recentTxId, userId, petShopCatId, CreateCustomRule: true, ApplyToPastTransactions: true);

        await _categorizeHandler.Handle(categorizeCommand, CancellationToken.None);

        // Assert: Past transaction must have been updated in batch
        var updatedPastTx = await _txRepo.GetByIdAsync(pastTxId, CancellationToken.None);
        updatedPastTx.Should().NotBeNull();
        updatedPastTx!.CategoryId.Should().Be(petShopCatId);
        updatedPastTx.IsManuallyCategorized.Should().BeTrue();
    }

    [Fact]
    public async Task CategorizeTransaction_WithCreateCustomRule_ShouldAutoCategorizeFutureIngestions()
    {
        // Arrange
        const string userId = "test-user-2";
        var petShopCatId = Guid.NewGuid();

        // 1. Ingest initial transaction
        var firstCommand = new IngestTransactionCommand(
            userId, "Inter", "acc-002", "tx-first-1", 50.00m, "BRL",
            TransactionType.Debit, "CUSCO BOX PORTO ALEGRE BRA",
            DateTime.UtcNow.AddDays(-2), TransactionChannel.Other, "CUSCO BOX PORTO ALEGRE BRA");

        var firstTxId = await _ingestHandler.Handle(firstCommand, CancellationToken.None);

        // 2. Categorize it and create a custom rule
        var categorizeCommand = new CategorizeTransactionCommand(
            firstTxId, userId, petShopCatId, CreateCustomRule: true, ApplyToPastTransactions: false);

        await _categorizeHandler.Handle(categorizeCommand, CancellationToken.None);

        // Act: Ingest a brand new future transaction with the same description pattern
        var futureCommand = new IngestTransactionCommand(
            userId, "Inter", "acc-002", "tx-future-1", 89.90m, "BRL",
            TransactionType.Debit, "CUSCO BOX PORTO ALEGRE BRA",
            DateTime.UtcNow.AddDays(1), TransactionChannel.Other, "CUSCO BOX PORTO ALEGRE BRA");

        var futureTxId = await _ingestHandler.Handle(futureCommand, CancellationToken.None);

        // Assert: Future transaction must be automatically categorized via UserRule
        var futureTx = await _txRepo.GetByIdAsync(futureTxId, CancellationToken.None);
        futureTx.Should().NotBeNull();
        futureTx!.CategoryId.Should().Be(petShopCatId);
        futureTx.CategorizationSource.Should().Be(CategorizationSource.UserRule);
        futureTx.IsManuallyCategorized.Should().BeFalse();
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
