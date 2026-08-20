using FinanceHub.PluggyIntegration.Application.DTOs;
using FinanceHub.PluggyIntegration.Application.Interfaces;
using FinanceHub.PluggyIntegration.Application.Services;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace FinanceHub.Tests.PluggyIntegration;

public class PluggyAggregationServiceTests
{
    private readonly IMeuPluggyClient _pluggyClient = Substitute.For<IMeuPluggyClient>();
    private readonly PluggyAggregationService _service;
    private const string ValidToken = "valid-token-123";

    public PluggyAggregationServiceTests()
    {
        _service = new PluggyAggregationService(_pluggyClient);
    }

    [Fact]
    public async Task FetchAllAccountsAsync_WhenItemsExist_ShouldFetchAccountsPerItemInParallel()
    {
        // Arrange
        var items = new List<PluggyItemDto>
        {
            new("item-1", "UPDATED", new(1, "Banco Itaú")),
            new("item-2", "UPDATED", new(2, "Banco Inter"))
        };

        var accountsItem1 = new List<PluggyAccountDto>
        {
            new("acc-1", "BANK", "CHECKING_ACCOUNT", "Itaú Conta", 100m, "BRL", "item-1", null)
        };

        var accountsItem2 = new List<PluggyAccountDto>
        {
            new("acc-2", "BANK", "CHECKING_ACCOUNT", "Inter Conta", 200m, "BRL", "item-2", null)
        };

        _pluggyClient.GetItemsAsync(ValidToken, Arg.Any<CancellationToken>()).Returns(items);
        _pluggyClient.GetAccountsByItemIdAsync("item-1", ValidToken, Arg.Any<CancellationToken>()).Returns(accountsItem1);
        _pluggyClient.GetAccountsByItemIdAsync("item-2", ValidToken, Arg.Any<CancellationToken>()).Returns(accountsItem2);

        // Act
        var result = await _service.FetchAllAccountsAsync(ValidToken, CancellationToken.None);

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(a => a.Id == "acc-1");
        result.Should().Contain(a => a.Id == "acc-2");
    }

    [Fact]
    public async Task FetchAllTransactionsAsync_WhenAccountsExist_ShouldFetchTransactionsPerAccountInParallel()
    {
        // Arrange
        var items = new List<PluggyItemDto> { new("item-1", "UPDATED", new(1, "Banco Itaú")) };
        var accounts = new List<PluggyAccountDto> { new("acc-1", "BANK", "CHECKING_ACCOUNT", "Itaú Conta", 100m, "BRL", "item-1", null) };
        var txs = new List<PluggyTransactionDto> { new("tx-1", "PIX", 50m, "2026-08-19T00:00:00Z", "CREDIT", "PIX", "acc-1") };

        _pluggyClient.GetItemsAsync(ValidToken, Arg.Any<CancellationToken>()).Returns(items);
        _pluggyClient.GetAccountsByItemIdAsync("item-1", ValidToken, Arg.Any<CancellationToken>()).Returns(accounts);
        _pluggyClient.GetTransactionsByAccountIdAsync("acc-1", ValidToken, Arg.Any<CancellationToken>()).Returns(txs);

        // Act
        var result = await _service.FetchAllTransactionsAsync(ValidToken, CancellationToken.None);

        // Assert
        result.Should().HaveCount(1);
        result[0].Id.Should().Be("tx-1");
    }
}
