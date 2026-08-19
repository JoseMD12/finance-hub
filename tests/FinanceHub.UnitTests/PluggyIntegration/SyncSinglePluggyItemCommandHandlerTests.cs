using FinanceHub.PluggyIntegration.Application.Commands.SyncAllPluggyAccounts;
using FinanceHub.PluggyIntegration.Application.Commands.SyncSinglePluggyItem;
using FinanceHub.PluggyIntegration.Application.DTOs;
using FinanceHub.PluggyIntegration.Application.Interfaces;
using FinanceHub.PluggyIntegration.Application.Services;
using FinanceHub.PluggyIntegration.Domain.Exceptions;
using FinanceHub.Shared.Messaging.Events;
using FluentAssertions;
using MassTransit;
using NSubstitute;
using Xunit;

namespace FinanceHub.UnitTests.PluggyIntegration;

public class SyncSinglePluggyItemCommandHandlerTests
{
    private readonly IMeuPluggyClient _pluggyClient = Substitute.For<IMeuPluggyClient>();
    private readonly IPluggyTransactionMapper _transactionMapper = new PluggyTransactionMapper();
    private readonly IPublishEndpoint _publishEndpoint = Substitute.For<IPublishEndpoint>();
    private readonly SyncSinglePluggyItemCommandHandler _handler;

    public SyncSinglePluggyItemCommandHandlerTests()
    {
        _handler = new SyncSinglePluggyItemCommandHandler(
            _pluggyClient,
            _transactionMapper,
            _publishEndpoint
        );
    }

    [Fact]
    public async Task HandleAsync_WhenTokenIsEmpty_ShouldThrowNullOrEmptyPluggyAccessTokenDomainException()
    {
        var command = new SyncSinglePluggyItemCommand("item-1", "user-1", "");
        var act = async () => await _handler.HandleAsync(command, CancellationToken.None);
        await act.Should().ThrowAsync<NullOrEmptyPluggyAccessTokenDomainException>();
    }

    [Fact]
    public async Task HandleAsync_WhenItemExists_ShouldSyncAccountsAndPublishEvents()
    {
        const string validToken = "valid-token";
        var items = new List<PluggyItemDto> { new("item-1", "UPDATED", new(1, "Banco Inter")) };
        var accounts = new List<PluggyAccountDto> { new("acc-1", "BANK", "CHECKING_ACCOUNT", "Inter Conta", 500m, "BRL", "item-1", null) };
        var txs = new List<PluggyTransactionDto> { new("tx-1", "PIX", 100m, "2026-08-19T00:00:00Z", "CREDIT", "PIX", "acc-1") };

        _pluggyClient.GetItemsAsync(validToken, Arg.Any<CancellationToken>()).Returns(items);
        _pluggyClient.GetAccountsByItemIdAsync("item-1", validToken, Arg.Any<CancellationToken>()).Returns(accounts);
        _pluggyClient.GetTransactionsByAccountIdAsync("acc-1", validToken, Arg.Any<CancellationToken>()).Returns(txs);

        var result = await _handler.HandleAsync(new SyncSinglePluggyItemCommand("item-1", "user-1", validToken), CancellationToken.None);

        result.Should().NotBeNull();
        result.TotalItemsSynced.Should().Be(1);
        result.TotalAccountsSynced.Should().Be(1);
        result.TotalCheckingTransactionsIngested.Should().Be(1);

        await _publishEndpoint.Received(1).Publish(Arg.Any<TransactionIngested>(), Arg.Any<CancellationToken>());
    }
}
