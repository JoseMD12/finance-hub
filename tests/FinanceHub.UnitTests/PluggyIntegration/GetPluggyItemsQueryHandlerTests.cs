using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using FinanceHub.PluggyIntegration.Application.DTOs;
using FinanceHub.PluggyIntegration.Application.Interfaces;
using FinanceHub.PluggyIntegration.Application.Queries.GetPluggyItems;
using FinanceHub.PluggyIntegration.Domain.Exceptions;
using NSubstitute;
using Xunit;

namespace FinanceHub.UnitTests.PluggyIntegration;

public class GetPluggyItemsQueryHandlerTests
{
    private readonly IMeuPluggyClient _pluggyClient = Substitute.For<IMeuPluggyClient>();
    private readonly GetPluggyItemsQueryHandler _handler;

    public GetPluggyItemsQueryHandlerTests()
    {
        _handler = new GetPluggyItemsQueryHandler(_pluggyClient);
    }

    [Fact]
    public async Task HandleAsync_WithValidToken_CalculatesOnlyBankAccountBalances_IgnoringCreditCards()
    {
        // Arrange
        const string validToken = "valid-session-jwt";
        var rawItems = new List<PluggyItemDto>
        {
            new("item-1", "UPDATED", new PluggyConnectorDto(1, "Itaú Unibanco")),
            new("item-2", "UPDATED", new PluggyConnectorDto(2, "Banco Inter"))
        };

        var accountsItem1 = new List<PluggyAccountDto>
        {
            new("acc-1", "BANK", "CHECKING_ACCOUNT", "Conta Corrente", 1500.50m, "BRL", "item-1", null),
            new("acc-2", "CREDIT", "CREDIT_CARD", "Cartão de Crédito", -300.00m, "BRL", "item-1",
                new PluggyCreditDataDto(700m, 1000m, null))
        };

        var accountsItem2 = new List<PluggyAccountDto>
        {
            new("acc-3", "BANK", "CHECKING_ACCOUNT", "Conta Digital", -250.00m, "BRL", "item-2", null)
        };

        _pluggyClient.GetItemsAsync(validToken, Arg.Any<CancellationToken>())
            .Returns(rawItems);

        _pluggyClient.GetAccountsByItemIdAsync("item-1", validToken, Arg.Any<CancellationToken>())
            .Returns(accountsItem1);

        _pluggyClient.GetAccountsByItemIdAsync("item-2", validToken, Arg.Any<CancellationToken>())
            .Returns(accountsItem2);

        var query = new GetPluggyItemsQuery(validToken);

        // Act
        var result = await _handler.HandleAsync(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);

        // Itaú should have 1500.50 (ignoring credit card balance of -300.00)
        result[0].Id.Should().Be("item-1");
        result[0].TotalBalance.Should().Be(1500.50m);
        result[0].TotalCredit.Should().Be(300m);
        result[0].AccountsCount.Should().Be(2);

        // Inter should preserve actual negative bank balance (-250.00)
        result[1].Id.Should().Be("item-2");
        result[1].TotalBalance.Should().Be(-250.00m);
        result[1].AccountsCount.Should().Be(1);

        await _pluggyClient.Received(1).GetItemsAsync(validToken, Arg.Any<CancellationToken>());
        await _pluggyClient.Received(1).GetAccountsByItemIdAsync("item-1", validToken, Arg.Any<CancellationToken>());
        await _pluggyClient.Received(1).GetAccountsByItemIdAsync("item-2", validToken, Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task HandleAsync_WithInvalidToken_ThrowsNullOrEmptyPluggyAccessTokenDomainException(string invalidToken)
    {
        // Arrange
        var query = new GetPluggyItemsQuery(invalidToken);

        // Act
        var act = () => _handler.HandleAsync(query, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NullOrEmptyPluggyAccessTokenDomainException>()
            .WithMessage("*AccessToken*");
    }
}
