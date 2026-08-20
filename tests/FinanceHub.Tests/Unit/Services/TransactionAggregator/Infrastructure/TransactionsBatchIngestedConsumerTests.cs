using FinanceHub.Shared.Messaging.Events;
using FinanceHub.TransactionAggregator.Application.Commands.IngestTransaction;
using FinanceHub.TransactionAggregator.Infrastructure.Messaging.Consumers;
using FluentAssertions;
using MassTransit;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace FinanceHub.Tests.Services.TransactionAggregator.Infrastructure;

public class TransactionsBatchIngestedConsumerTests
{
    private readonly IIngestTransactionCommandHandler _handler = Substitute.For<IIngestTransactionCommandHandler>();
    private readonly TransactionsBatchIngestedConsumer _consumer;

    public TransactionsBatchIngestedConsumerTests()
    {
        _consumer = new TransactionsBatchIngestedConsumer(_handler, NullLogger<TransactionsBatchIngestedConsumer>.Instance);
    }

    [Fact]
    public async Task Consume_WhenTransactionsBatchIngestedReceived_ShouldInvokeHandlerForEachTransactionInBatch()
    {
        // Arrange
        var checkingTx = new TransactionIngested(
            IngestionId: Guid.NewGuid(),
            UserId: "user-01",
            Source: "Banco Inter",
            AccountId: "acc-inter-checking",
            BankTransactionId: "tx-1",
            Amount: 150.00m,
            TransactionDate: DateTime.UtcNow,
            Description: "PIX Recebido",
            Currency: "BRL",
            RawPayloadJson: "{}",
            OccurredAtUtc: DateTime.UtcNow
        );

        var cardTx = new InvoiceItemIngested(
            IngestionId: Guid.NewGuid(),
            UserId: "user-01",
            Source: "Banco Inter",
            CreditCardAccountId: "acc-inter-card",
            CardLastFourDigits: "1234",
            BankTransactionId: "tx-2",
            Amount: 50.00m,
            TransactionDate: DateTime.UtcNow,
            Description: "Supermercado",
            Category: "Alimentação",
            CurrentInstallment: 1,
            TotalInstallments: 1,
            InvoiceDueDate: DateTime.UtcNow.AddDays(5),
            Currency: "BRL",
            RawPayloadJson: "{}",
            OccurredAtUtc: DateTime.UtcNow
        );

        var batchEvent = new TransactionsBatchIngested(
            BatchId: Guid.NewGuid(),
            UserId: "user-01",
            ChunkIndex: 1,
            TotalChunks: 1,
            CheckingTransactions: new[] { checkingTx },
            CardTransactions: new[] { cardTx },
            OccurredAtUtc: DateTime.UtcNow
        );

        var consumeContext = Substitute.For<ConsumeContext<TransactionsBatchIngested>>();
        consumeContext.Message.Returns(batchEvent);
        consumeContext.CancellationToken.Returns(CancellationToken.None);

        _handler.Handle(Arg.Any<IngestTransactionCommand>(), Arg.Any<CancellationToken>())
            .Returns(Guid.NewGuid());

        // Act
        await _consumer.Consume(consumeContext);

        // Assert
        await _handler.Received(2).Handle(Arg.Any<IngestTransactionCommand>(), Arg.Any<CancellationToken>());
    }
}
