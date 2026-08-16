using FinanceHub.MercadoPagoIntegration.Application.Commands.SyncTransactions;
using FinanceHub.MercadoPagoIntegration.Application.Interfaces;
using FinanceHub.MercadoPagoIntegration.Domain.Entities;
using FinanceHub.MercadoPagoIntegration.Domain.Exceptions;
using FinanceHub.Shared.Connectors;
using FinanceHub.Shared.Messaging.Events;
using FluentAssertions;
using MassTransit;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using Xunit;

namespace FinanceHub.UnitTests.Services.MercadoPagoIntegration.Application;

public class SyncMercadoPagoOpenFinanceCommandHandlerTests
{
    private readonly IOpenFinanceClient _client = Substitute.For<IOpenFinanceClient>();
    private readonly IMercadoPagoSyncStateRepository _repository = Substitute.For<IMercadoPagoSyncStateRepository>();
    private readonly IPublishEndpoint _publishEndpoint = Substitute.For<IPublishEndpoint>();
    private readonly FakeTimeProvider _timeProvider = new();

    private readonly SyncMercadoPagoOpenFinanceCommandHandler _handler;

    public SyncMercadoPagoOpenFinanceCommandHandlerTests()
    {
        _timeProvider.SetUtcNow(new DateTimeOffset(2026, 8, 16, 12, 0, 0, TimeSpan.Zero));
        _handler = new SyncMercadoPagoOpenFinanceCommandHandler(
            _client,
            _repository,
            _publishEndpoint,
            _timeProvider,
            NullLogger<SyncMercadoPagoOpenFinanceCommandHandler>.Instance
        );
    }

    [Fact]
    public async Task Handle_WithNoAccounts_ShouldThrowOpenFinanceItemNotFoundDomainException()
    {
        // Arrange
        _client.GetAccountsByItemAsync("item-empty", Arg.Any<CancellationToken>())
            .Returns(new List<BankAccountDto>().AsReadOnly());

        // Act
        var act = () => _handler.Handle(new SyncMercadoPagoOpenFinanceCommand("user-01", "item-empty"), CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<OpenFinanceItemNotFoundDomainException>();
    }

    [Fact]
    public async Task Handle_WithValidAccountAndTransactions_ShouldPublishEventsAndCompleteSync()
    {
        // Arrange
        var accounts = new List<BankAccountDto>
        {
            new("acc-mp-01", "mercadopago", "PAYMENT", "BRL", "Mercado Pago Conta")
        };

        _client.GetAccountsByItemAsync("item-01", Arg.Any<CancellationToken>())
            .Returns(accounts.AsReadOnly());

        var transactions = new List<BankTransactionDto>
        {
            new(
                TransactionId: "tx-mp-01",
                AccountId: "acc-mp-01",
                Amount: -75.50m,
                Currency: "BRL",
                BookingDateTime: _timeProvider.GetUtcNow().AddDays(-2),
                TransactionInformation: "Pix Enviado - Mercado Pago",
                CreditDebitIndicator: "DBIT",
                FeeAmount: null,
                RawPayload: "{}"
            )
        };

        _client.GetTransactionsByAccountAsync("acc-mp-01", Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(transactions.AsReadOnly());

        // Act
        var result = await _handler.Handle(new SyncMercadoPagoOpenFinanceCommand("user-01", "item-01"), CancellationToken.None);

        // Assert
        result.IngestedCount.Should().Be(1);
        result.Status.Should().Be("Completed");

        await _publishEndpoint.Received(1).Publish(Arg.Is<TransactionIngested>(e => e.BankTransactionId == "tx-mp-01" && e.Amount == -75.50m), Arg.Any<CancellationToken>());
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
