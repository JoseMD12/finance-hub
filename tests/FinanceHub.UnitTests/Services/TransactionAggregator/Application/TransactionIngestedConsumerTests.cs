using FinanceHub.Shared.Messaging.Events;
using FinanceHub.TransactionAggregator.Application.Commands.IngestTransaction;
using FinanceHub.TransactionAggregator.Application.Consumers;
using FinanceHub.TransactionAggregator.Domain.Entities;
using FluentAssertions;
using MassTransit;
using NSubstitute;
using Xunit;

namespace FinanceHub.UnitTests.Services.TransactionAggregator.Application;

public class TransactionIngestedConsumerTests
{
    private readonly IIngestTransactionCommandHandler _commandHandler = Substitute.For<IIngestTransactionCommandHandler>();
    private readonly ConsumeContext<TransactionIngested> _consumeContext = Substitute.For<ConsumeContext<TransactionIngested>>();
    private readonly TransactionIngestedConsumer _consumer;

    public TransactionIngestedConsumerTests()
    {
        _consumer = new TransactionIngestedConsumer(_commandHandler);
    }

    [Fact]
    public async Task Consume_WithValidCreditTransaction_ShouldCallIngestCommandHandlerWithCorrectParameters()
    {
        // Arrange
        var message = new TransactionIngested(
            IngestionId: Guid.NewGuid(),
            UserId: "user-123",
            Source: "mercadopago",
            AccountId: "collector-456",
            BankTransactionId: "tx-789",
            Amount: 150.50m,
            TransactionDate: new DateTime(2026, 8, 15, 12, 0, 0, DateTimeKind.Utc),
            Description: "Pix Recebido",
            Currency: "BRL",
            RawPayloadJson: "{\"id\":\"tx-789\"}",
            OccurredAtUtc: DateTime.UtcNow
        );

        _consumeContext.Message.Returns(message);
        _consumeContext.CancellationToken.Returns(CancellationToken.None);

        _commandHandler.Handle(Arg.Any<IngestTransactionCommand>(), Arg.Any<CancellationToken>())
            .Returns(Guid.NewGuid());

        // Act
        await _consumer.Consume(_consumeContext);

        // Assert
        await _commandHandler.Received(1).Handle(
            Arg.Is<IngestTransactionCommand>(cmd =>
                cmd.UserId == "user-123" &&
                cmd.InstitutionId == "mercadopago" &&
                cmd.AccountNumber == "collector-456" &&
                cmd.BankTransactionId == "tx-789" &&
                cmd.Amount == 150.50m &&
                cmd.Currency == "BRL" &&
                cmd.Type == TransactionType.Credit &&
                cmd.RawDescription == "Pix Recebido"
            ),
            Arg.Any<CancellationToken>()
        );
    }

    [Fact]
    public async Task Consume_WithNegativeAmountDebitTransaction_ShouldMapToDebitTransactionType()
    {
        // Arrange
        var message = new TransactionIngested(
            IngestionId: Guid.NewGuid(),
            UserId: "user-123",
            Source: "mercadopago",
            AccountId: "collector-456",
            BankTransactionId: "tx-debit-001",
            Amount: -75.25m,
            TransactionDate: new DateTime(2026, 8, 15, 14, 0, 0, DateTimeKind.Utc),
            Description: "Pagamento Mercado Pago",
            Currency: "BRL",
            RawPayloadJson: "{}",
            OccurredAtUtc: DateTime.UtcNow
        );

        _consumeContext.Message.Returns(message);

        // Act
        await _consumer.Consume(_consumeContext);

        // Assert
        await _commandHandler.Received(1).Handle(
            Arg.Is<IngestTransactionCommand>(cmd =>
                cmd.Amount == 75.25m && // Canonical positive magnitude with Debit type
                cmd.Type == TransactionType.Debit
            ),
            Arg.Any<CancellationToken>()
        );
    }
}
