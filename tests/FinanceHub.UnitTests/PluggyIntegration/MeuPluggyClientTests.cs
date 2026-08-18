using System.Net;
using FinanceHub.PluggyIntegration.Application.DTOs;
using FinanceHub.PluggyIntegration.Domain.Exceptions;
using FinanceHub.PluggyIntegration.Infrastructure.Clients;
using FinanceHub.PluggyIntegration.Infrastructure.Configuration;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace FinanceHub.UnitTests.PluggyIntegration;

public class MeuPluggyClientTests
{
    private const string ValidToken = "valid-token-123";

    private class MockHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _handler;

        public MockHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> handler)
        {
            _handler = handler;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(_handler(request));
        }
    }

    [Fact]
    public async Task GetItemsAsync_WhenApiReturns200_ShouldReturnItems()
    {
        // Arrange
        var jsonResponse = """
        [
            {
                "id": "item-123",
                "status": "UPDATED",
                "connector": {
                    "id": 606,
                    "name": "Mercado Pago"
                }
            }
        ]
        """;

        var handler = new MockHttpMessageHandler(req =>
        {
            req.RequestUri!.PathAndQuery.Should().Be("/items");
            req.Headers.Authorization!.Scheme.Should().Be("Bearer");
            req.Headers.Authorization.Parameter.Should().Be(ValidToken);

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(jsonResponse, System.Text.Encoding.UTF8, "application/json")
            };
        });

        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://my-api.pluggy.ai") };
        var options = Options.Create(new PluggyOptions { ApiBaseUrl = "https://my-api.pluggy.ai" });
        var client = new MeuPluggyClient(httpClient, options, NullLogger<MeuPluggyClient>.Instance);

        // Act
        var items = await client.GetItemsAsync(ValidToken);

        // Assert
        items.Should().HaveCount(1);
        items[0].Id.Should().Be("item-123");
        items[0].Status.Should().Be("UPDATED");
        items[0].Connector.Name.Should().Be("Mercado Pago");
    }

    [Fact]
    public async Task GetItemsAsync_WhenTokenIsMissing_ShouldThrowNullOrEmptyPluggyAccessTokenDomainException()
    {
        // Arrange
        var handler = new MockHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://my-api.pluggy.ai") };
        var options = Options.Create(new PluggyOptions { ApiBaseUrl = "https://my-api.pluggy.ai" });
        var client = new MeuPluggyClient(httpClient, options, NullLogger<MeuPluggyClient>.Instance);

        // Act
        var act = async () => await client.GetItemsAsync(string.Empty);

        // Assert
        await act.Should().ThrowAsync<NullOrEmptyPluggyAccessTokenDomainException>()
            .WithMessage("*X-Pluggy-Access-Token*");
    }

    [Fact]
    public async Task GetItemsAsync_WhenApiReturns401_ShouldThrowPluggySessionExpiredDomainException()
    {
        // Arrange
        var handler = new MockHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.Unauthorized));
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://my-api.pluggy.ai") };
        var options = Options.Create(new PluggyOptions { ApiBaseUrl = "https://my-api.pluggy.ai" });
        var client = new MeuPluggyClient(httpClient, options, NullLogger<MeuPluggyClient>.Instance);

        // Act
        var act = async () => await client.GetItemsAsync(ValidToken);

        // Assert
        await act.Should().ThrowAsync<PluggySessionExpiredDomainException>()
            .WithMessage("*expirou*");
    }

    [Fact]
    public async Task GetAccountsByItemIdAsync_WhenApiReturnsAccounts_ShouldReturnParsedAccounts()
    {
        // Arrange
        var jsonResponse = """
        [
            {
                "id": "acc-checking-01",
                "type": "BANK",
                "subtype": "CHECKING_ACCOUNT",
                "name": "BANCO INTER",
                "balance": 97.60,
                "currencyCode": "BRL",
                "itemId": "item-inter-01"
            }
        ]
        """;

        var handler = new MockHttpMessageHandler(req =>
        {
            req.RequestUri!.PathAndQuery.Should().Be("/accounts?itemId=item-inter-01");
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(jsonResponse, System.Text.Encoding.UTF8, "application/json")
            };
        });

        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://my-api.pluggy.ai") };
        var options = Options.Create(new PluggyOptions { ApiBaseUrl = "https://my-api.pluggy.ai" });
        var client = new MeuPluggyClient(httpClient, options, NullLogger<MeuPluggyClient>.Instance);

        // Act
        var accounts = await client.GetAccountsByItemIdAsync("item-inter-01", ValidToken);

        // Assert
        accounts.Should().HaveCount(1);
        accounts[0].Subtype.Should().Be("CHECKING_ACCOUNT");
        accounts[0].Balance.Should().Be(97.60m);
    }

    [Fact]
    public async Task GetTransactionsByAccountIdAsync_WhenApiReturnsTransactions_ShouldReturnParsedTransactions()
    {
        // Arrange
        var jsonResponse = """
        [
            {
                "id": "tx-001",
                "description": "Transferência recebida - Fundatec",
                "amount": 97.60,
                "date": "2026-08-14T00:00:00.000Z",
                "type": "CREDIT",
                "category": "Transfer - PIX"
            }
        ]
        """;

        var handler = new MockHttpMessageHandler(req =>
        {
            req.RequestUri!.PathAndQuery.Should().Be("/transactions?accountId=acc-123");
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(jsonResponse, System.Text.Encoding.UTF8, "application/json")
            };
        });

        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://my-api.pluggy.ai") };
        var options = Options.Create(new PluggyOptions { ApiBaseUrl = "https://my-api.pluggy.ai" });
        var client = new MeuPluggyClient(httpClient, options, NullLogger<MeuPluggyClient>.Instance);

        // Act
        var txs = await client.GetTransactionsByAccountIdAsync("acc-123", ValidToken);

        // Assert
        txs.Should().HaveCount(1);
        txs[0].Description.Should().Be("Transferência recebida - Fundatec");
        txs[0].Amount.Should().Be(97.60m);
    }
}
