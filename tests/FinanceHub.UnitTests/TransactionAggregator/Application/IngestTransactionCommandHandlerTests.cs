using System;
using System.Threading;
using System.Threading.Tasks;
using FinanceHub.TransactionAggregator.Application.Commands.IngestTransaction;
using FinanceHub.TransactionAggregator.Application.Interfaces;
using FinanceHub.TransactionAggregator.Application.Services.Categorization;
using FinanceHub.TransactionAggregator.Domain.Entities;
using FinanceHub.TransactionAggregator.Domain.ValueObjects;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace FinanceHub.UnitTests.TransactionAggregator.Application;

public class IngestTransactionCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenTransactionDoesNotExist_ShouldCategorizeIngestAndApplyBalance()
    {
        // Arrange
        var txRepo = Substitute.For<ITransactionRepository>();
        var balanceRepo = Substitute.For<IAccountBalanceRepository>();
        var pipeline = Substitute.For<ICategoryResolverPipeline>();
        var categoryId = Guid.NewGuid();

        txRepo.ExistsByHashAsync(Arg.Any<TransactionHash>(), Arg.Any<CancellationToken>())
            .Returns(false);

        pipeline.ResolveCategoryAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new CategorizationResult(categoryId, CategorizationSource.GlobalRule));

        var handler = new IngestTransactionCommandHandler(txRepo, balanceRepo, pipeline);

        var command = new IngestTransactionCommand(
            "user-1",
            "itau",
            "acc-100",
            "bank-tx-99",
            150.75m,
            "BRL",
            TransactionType.Debit,
            "PAG*Supermercado 12/08",
            DateTime.UtcNow,
            TransactionChannel.DebitCard,
            "Supermercado");

        // Act
        var resultId = await handler.Handle(command, CancellationToken.None);

        // Assert
        resultId.Should().NotBeEmpty();
        await txRepo.Received(1).AddAsync(Arg.Is<CanonicalTransaction>(t =>
            t.UserId == "user-1" &&
            t.Amount.Amount == 150.75m &&
            t.CategoryId == categoryId), Arg.Any<CancellationToken>());

        await balanceRepo.Received(1).AddOrUpdateAsync(Arg.Any<AccountBalance>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenTransactionAlreadyExistsByHash_ShouldDeduplicateAndReturnExistingId()
    {
        // Arrange
        var txRepo = Substitute.For<ITransactionRepository>();
        var balanceRepo = Substitute.For<IAccountBalanceRepository>();
        var pipeline = Substitute.For<ICategoryResolverPipeline>();

        var existingTxId = Guid.NewGuid();
        txRepo.ExistsByHashAsync(Arg.Any<TransactionHash>(), Arg.Any<CancellationToken>())
            .Returns(true);
        txRepo.GetIdByHashAsync(Arg.Any<TransactionHash>(), Arg.Any<CancellationToken>())
            .Returns(existingTxId);

        var handler = new IngestTransactionCommandHandler(txRepo, balanceRepo, pipeline);

        var command = new IngestTransactionCommand(
            "user-1",
            "itau",
            "acc-100",
            "bank-tx-99",
            150.75m,
            "BRL",
            TransactionType.Debit,
            "PAG*Supermercado 12/08",
            DateTime.UtcNow,
            TransactionChannel.DebitCard,
            "Supermercado");

        // Act
        var resultId = await handler.Handle(command, CancellationToken.None);

        // Assert
        resultId.Should().Be(existingTxId);
        await txRepo.DidNotReceive().AddAsync(Arg.Any<CanonicalTransaction>(), Arg.Any<CancellationToken>());
    }
}
