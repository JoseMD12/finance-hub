using FinanceHub.PluggyIntegration.Application.Commands.SyncAllPluggyAccounts;
using FinanceHub.PluggyIntegration.Application.DTOs;
using FinanceHub.PluggyIntegration.Application.Interfaces;
using FinanceHub.Shared.Messaging.Events;
using FluentAssertions;
using MassTransit;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace FinanceHub.UnitTests.PluggyIntegration;

public class SyncAllPluggyAccountsCommandHandlerTests
{
    private readonly IMeuPluggyClient _pluggyClient = Substitute.For<IMeuPluggyClient>();
    private readonly IPublishEndpoint _publishEndpoint = Substitute.For<IPublishEndpoint>();
    private readonly SyncAllPluggyAccountsCommandHandler _handler;

    public SyncAllPluggyAccountsCommandHandlerTests()
    {
        _handler = new SyncAllPluggyAccountsCommandHandler(
            _pluggyClient,
            _publishEndpoint,
            NullLogger<SyncAllPluggyAccountsCommandHandler>.Instance
        );
    }

    [Fact]
    public async Task HandleAsync_WhenItemsAndAccountsExist_ShouldPublishCorrectEventsAndReturnSummary()
    {
        // Arrange
        var items = new List<PluggyItemDto>
        {
            new("item-inter-1", "UPDATED", new(77, "Banco Inter")),
            new("item-itau-2", "UPDATED", new(341, "Banco Itaú"))
        };

        var interAccounts = new List<PluggyAccountDto>
        {
            new("acc-inter-checking", "BANK", "CHECKING_ACCOUNT", "Inter Conta", 97.60m, "BRL", "item-inter-1", null),
            new("acc-inter-card", "CREDIT", "CREDIT_CARD", "Inter Gold", 1711.19m, "BRL", "item-inter-1", new(3000, 5000, "2026-08-20"))
        };

        var itauAccounts = new List<PluggyAccountDto>
        {
            new("acc-itau-checking", "BANK", "CHECKING_ACCOUNT", "Itaú Conta", 211.00m, "BRL", "item-itau-2", null)
        };

        var checkingTxs = new List<PluggyTransactionDto>
        {
            new("tx-1", "Transferência recebida - Fundatec", 97.60m, "2026-08-14T00:00:00Z", "CREDIT", "Transfer - PIX")
        };

        var cardTxs = new List<PluggyTransactionDto>
        {
            new("tx-2", "MCDONALDS", 40.00m, "2026-08-15T00:00:00Z", "DEBIT", "Eating out")
        };

        _pluggyClient.GetItemsAsync(Arg.Any<CancellationToken>()).Returns(items);
        _pluggyClient.GetAccountsByItemIdAsync("item-inter-1", Arg.Any<CancellationToken>()).Returns(interAccounts);
        _pluggyClient.GetAccountsByItemIdAsync("item-itau-2", Arg.Any<CancellationToken>()).Returns(itauAccounts);
        _pluggyClient.GetTransactionsByAccountIdAsync("acc-inter-checking", Arg.Any<CancellationToken>()).Returns(checkingTxs);
        _pluggyClient.GetTransactionsByAccountIdAsync("acc-inter-card", Arg.Any<CancellationToken>()).Returns(cardTxs);
        _pluggyClient.GetTransactionsByAccountIdAsync("acc-itau-checking", Arg.Any<CancellationToken>()).Returns(new List<PluggyTransactionDto>());

        // Act
        var result = await _handler.HandleAsync(new SyncAllPluggyAccountsCommand("user-01"), CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.TotalItemsSynced.Should().Be(2);
        result.TotalAccountsSynced.Should().Be(3);
        result.TotalCheckingTransactionsIngested.Should().Be(1);
        result.TotalCardTransactionsIngested.Should().Be(1);

        // Verify MassTransit Published Events
        await _publishEndpoint.Received(1).Publish(
            Arg.Is<TransactionIngested>(e => e.Source == "Banco Inter" && e.Amount == 97.60m && e.AccountId == "acc-inter-checking" && e.UserId == "user-01"),
            Arg.Any<CancellationToken>()
        );

        await _publishEndpoint.Received(1).Publish(
            Arg.Is<InvoiceItemIngested>(e => e.Source == "Banco Inter" && e.Amount == 40.00m && e.Category == "Alimentação" && e.UserId == "user-01"),
            Arg.Any<CancellationToken>()
        );
    }
}
