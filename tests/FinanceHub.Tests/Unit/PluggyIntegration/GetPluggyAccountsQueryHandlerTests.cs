using FluentAssertions;
using FinanceHub.PluggyIntegration.Application.DTOs;
using FinanceHub.PluggyIntegration.Application.Interfaces;
using FinanceHub.PluggyIntegration.Application.Queries.GetPluggyAccounts;
using FinanceHub.PluggyIntegration.Domain.Exceptions;
using NSubstitute;
using Xunit;

namespace FinanceHub.Tests.PluggyIntegration;

public class GetPluggyAccountsQueryHandlerTests
{
    private readonly IMeuPluggyClient _pluggyClient = Substitute.For<IMeuPluggyClient>();
    private readonly GetPluggyAccountsQueryHandler _handler;

    public GetPluggyAccountsQueryHandlerTests()
    {
        _handler = new GetPluggyAccountsQueryHandler(_pluggyClient);
    }

    [Fact]
    public async Task HandleAsync_WithValidToken_ReturnsAccountsWithInstitutionContext()
    {
        const string validToken = "valid-session-jwt";
        var item = new PluggyItemDto("item-1", "UPDATED", new PluggyConnectorDto(1, "Itaú Unibanco"));
        var itemAccounts = new List<PluggyAccountDto>
        {
            new("account-1", "BANK", "CHECKING_ACCOUNT", "Conta Corrente", 1500m, "BRL", "item-1", null),
            new("account-2", "CREDIT", "CREDIT_CARD", "Cartão Visa", -250m, "BRL", "item-1",
                new PluggyCreditDataDto(750m, 1000m, null))
        };

        _pluggyClient.GetItemsAsync(validToken, Arg.Any<CancellationToken>()).Returns([item]);
        _pluggyClient.GetAccountsByItemIdAsync("item-1", validToken, Arg.Any<CancellationToken>())
            .Returns(itemAccounts);

        var result = await _handler.HandleAsync(
            new GetPluggyAccountsQuery(validToken),
            CancellationToken.None);

        result.Should().HaveCount(2);
        result[0].InstitutionName.Should().Be("Itaú Unibanco");
        result[0].Name.Should().Be("Conta Corrente");
        result[0].Subtype.Should().Be("CHECKING_ACCOUNT");
        var creditData = result[1].CreditData;
        creditData.Should().NotBeNull();
        creditData!.CreditLimit.Should().Be(1000m);
        creditData.AvailableCreditLimit.Should().Be(750m);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task HandleAsync_WithInvalidToken_ThrowsNullOrEmptyPluggyAccessTokenDomainException(string invalidToken)
    {
        var act = () => _handler.HandleAsync(
            new GetPluggyAccountsQuery(invalidToken),
            CancellationToken.None);

        await act.Should().ThrowAsync<NullOrEmptyPluggyAccessTokenDomainException>();
    }
}
