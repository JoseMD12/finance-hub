using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FinanceHub.TransactionAggregator.Application.Interfaces;
using FinanceHub.TransactionAggregator.Application.Services;
using FinanceHub.TransactionAggregator.Domain.Entities;
using FinanceHub.TransactionAggregator.Domain.ValueObjects;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace FinanceHub.Tests.TransactionAggregator.Application;

public class TransferPairMatchingEngineTests
{
    private readonly ITransactionRepository _repository;
    private readonly ILogger<TransferPairMatchingEngine> _logger;
    private readonly TransferPairMatchingEngine _engine;

    public TransferPairMatchingEngineTests()
    {
        _repository = Substitute.For<ITransactionRepository>();
        _logger = Substitute.For<ILogger<TransferPairMatchingEngine>>();
        _engine = new TransferPairMatchingEngine(_repository, _logger);
    }

    [Fact]
    public async Task MatchAndPairAsync_WhenOppositeTransactionsWithin96Hours_ShouldPairBothAndSetNatureToTransfer()
    {
        // Arrange
        var userId = "user-123";
        var now = DateTime.UtcNow;

        var debitTx = CreateTransaction(
            userId,
            "itau",
            "acc-1",
            1000m,
            TransactionType.Debit,
            "PIX ENVIADO - BANCO INTER",
            now);

        var creditTx = CreateTransaction(
            userId,
            "inter",
            "acc-2",
            1000m,
            TransactionType.Credit,
            "PIX RECEBIDO - ITAU",
            now.AddHours(2));

        var candidates = new List<CanonicalTransaction> { debitTx, creditTx };

        _repository.GetUnpairedTransfersCandidateAsync(userId, Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(candidates);

        // Act
        var pairedCount = await _engine.MatchAndPairAsync(userId, CancellationToken.None);

        // Assert
        pairedCount.Should().Be(1);
        debitTx.Nature.Should().Be(TransactionNature.Transfer);
        debitTx.IsIgnoredInTotals.Should().BeTrue();
        debitTx.PairedTransactionId.Should().Be(creditTx.Id);

        creditTx.Nature.Should().Be(TransactionNature.Transfer);
        creditTx.IsIgnoredInTotals.Should().BeTrue();
        creditTx.PairedTransactionId.Should().Be(debitTx.Id);

        await _repository.Received(1).UpdateRangeAsync(
            Arg.Is<IEnumerable<CanonicalTransaction>>(list => list.Count() == 2),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task MatchAndPairAsync_WhenAmountsDiffer_ShouldNotPair()
    {
        // Arrange
        var userId = "user-123";
        var now = DateTime.UtcNow;

        var debitTx = CreateTransaction(userId, "itau", "acc-1", 1000m, TransactionType.Debit, "PIX ENVIADO", now);
        var creditTx = CreateTransaction(userId, "inter", "acc-2", 990m, TransactionType.Credit, "PIX RECEBIDO", now.AddHours(1));

        _repository.GetUnpairedTransfersCandidateAsync(userId, Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(new List<CanonicalTransaction> { debitTx, creditTx });

        // Act
        var pairedCount = await _engine.MatchAndPairAsync(userId, CancellationToken.None);

        // Assert
        pairedCount.Should().Be(0);
        debitTx.PairedTransactionId.Should().BeNull();
        creditTx.PairedTransactionId.Should().BeNull();
        await _repository.DidNotReceive().UpdateRangeAsync(Arg.Any<IEnumerable<CanonicalTransaction>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task MatchAndPairAsync_WhenTimeDifferenceExceeds96Hours_ShouldNotPair()
    {
        // Arrange
        var userId = "user-123";
        var now = DateTime.UtcNow;

        var debitTx = CreateTransaction(userId, "itau", "acc-1", 1000m, TransactionType.Debit, "PIX ENVIADO", now);
        var creditTx = CreateTransaction(userId, "inter", "acc-2", 1000m, TransactionType.Credit, "PIX RECEBIDO", now.AddHours(100)); // > 96h

        _repository.GetUnpairedTransfersCandidateAsync(userId, Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(new List<CanonicalTransaction> { debitTx, creditTx });

        // Act
        var pairedCount = await _engine.MatchAndPairAsync(userId, CancellationToken.None);

        // Assert
        pairedCount.Should().Be(0);
        debitTx.PairedTransactionId.Should().BeNull();
        creditTx.PairedTransactionId.Should().BeNull();
    }

    [Fact]
    public async Task MatchAndPairAsync_WhenSameAccount_ShouldNotPair()
    {
        // Arrange
        var userId = "user-123";
        var now = DateTime.UtcNow;

        var debitTx = CreateTransaction(userId, "itau", "acc-1", 1000m, TransactionType.Debit, "PIX ENVIADO", now);
        var creditTx = CreateTransaction(userId, "itau", "acc-1", 1000m, TransactionType.Credit, "PIX RECEBIDO", now.AddHours(1));

        _repository.GetUnpairedTransfersCandidateAsync(userId, Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(new List<CanonicalTransaction> { debitTx, creditTx });

        // Act
        var pairedCount = await _engine.MatchAndPairAsync(userId, CancellationToken.None);

        // Assert
        pairedCount.Should().Be(0);
    }

    private static CanonicalTransaction CreateTransaction(
        string userId,
        string institutionId,
        string accountNumber,
        decimal amount,
        TransactionType type,
        string description,
        DateTime dateUtc)
    {
        var randomHash = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N");
        var creationParams = new CanonicalTransactionCreationParams(
            userId,
            new AccountIdentifier(institutionId, accountNumber),
            new TransactionHash(randomHash),
            new Money(amount, "BRL"),
            type,
            SanitizedDescription.Create(description),
            Guid.NewGuid(),
            CategorizationSource.GlobalRule,
            dateUtc,
            new BankTransactionDetails("tx-1", TransactionChannel.Pix, string.Empty));

        return CanonicalTransaction.Create(creationParams);
    }
}
