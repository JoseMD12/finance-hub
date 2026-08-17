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
            req.Headers.Authorization.Parameter.Should().Be("valid-token");

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(jsonResponse, System.Text.Encoding.UTF8, "application/json")
            };
        });

        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://my-api.pluggy.ai") };
        var options = Options.Create(new PluggyOptions { UserToken = "valid-token", ApiBaseUrl = "https://my-api.pluggy.ai" });
        var client = new MeuPluggyClient(httpClient, options, NullLogger<MeuPluggyClient>.Instance);

        // Act
        var items = await client.GetItemsAsync();

        // Assert
        items.Should().HaveCount(1);
        items[0].Id.Should().Be("item-123");
        items[0].Status.Should().Be("UPDATED");
        items[0].Connector.Name.Should().Be("Mercado Pago");
    }

    [Fact]
    public async Task GetItemsAsync_WhenApiReturns401_ShouldThrowPluggySessionExpiredDomainException()
    {
        // Arrange
        var handler = new MockHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.Unauthorized));
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://my-api.pluggy.ai") };
        var options = Options.Create(new PluggyOptions { UserToken = "expired-token", ApiBaseUrl = "https://my-api.pluggy.ai" });
        var client = new MeuPluggyClient(httpClient, options, NullLogger<MeuPluggyClient>.Instance);

        // Act
        var act = async () => await client.GetItemsAsync();

        // Assert
        await act.Should().ThrowAsync<PluggySessionExpiredDomainException>()
            .WithMessage("*expirou*");
    }

    [Fact]
    public async Task GetItemsAsync_WhenApiReturns429_ShouldThrowPluggyRateLimitDomainException()
    {
        // Arrange
        var handler = new MockHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.TooManyRequests));
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://my-api.pluggy.ai") };
        var options = Options.Create(new PluggyOptions { UserToken = "valid-token", ApiBaseUrl = "https://my-api.pluggy.ai" });
        var client = new MeuPluggyClient(httpClient, options, NullLogger<MeuPluggyClient>.Instance);

        // Act
        var act = async () => await client.GetItemsAsync();

        // Assert
        await act.Should().ThrowAsync<PluggyRateLimitDomainException>()
            .WithMessage("*Limite de requisições excedido*");
    }

    [Fact]
    public async Task GetItemsAsync_WhenApiReturns500_ShouldThrowPluggyApiCommunicationDomainException()
    {
        // Arrange
        var handler = new MockHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError)
        {
            Content = new StringContent("Downstream failure")
        });
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://my-api.pluggy.ai") };
        var options = Options.Create(new PluggyOptions { UserToken = "valid-token", ApiBaseUrl = "https://my-api.pluggy.ai" });
        var client = new MeuPluggyClient(httpClient, options, NullLogger<MeuPluggyClient>.Instance);

        // Act
        var act = async () => await client.GetItemsAsync();

        // Assert
        await act.Should().ThrowAsync<PluggyApiCommunicationDomainException>()
            .WithMessage("*Erro HTTP 500*");
    }

    [Fact]
    public async Task GetItemsAsync_WhenNetworkThrowsException_ShouldThrowPluggyApiCommunicationDomainException()
    {
        // Arrange
        var handler = new MockHttpMessageHandler(_ => throw new HttpRequestException("Connection refused"));
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://my-api.pluggy.ai") };
        var options = Options.Create(new PluggyOptions { UserToken = "valid-token", ApiBaseUrl = "https://my-api.pluggy.ai" });
        var client = new MeuPluggyClient(httpClient, options, NullLogger<MeuPluggyClient>.Instance);

        // Act
        var act = async () => await client.GetItemsAsync();

        // Assert
        await act.Should().ThrowAsync<PluggyApiCommunicationDomainException>()
            .WithMessage("*Não foi possível conectar*");
    }

    [Fact]
    public async Task GetItemsAsync_WhenTokenIsMissing_ShouldThrowPluggySessionExpiredDomainException()
    {
        // Arrange
        var handler = new MockHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://my-api.pluggy.ai") };
        var options = Options.Create(new PluggyOptions { UserToken = string.Empty, ApiBaseUrl = "https://my-api.pluggy.ai" });
        var client = new MeuPluggyClient(httpClient, options, NullLogger<MeuPluggyClient>.Instance);

        // Act
        var act = async () => await client.GetItemsAsync();

        // Assert
        await act.Should().ThrowAsync<PluggySessionExpiredDomainException>()
            .WithMessage("*PLUGGY_USER_TOKEN não foi informada*");
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
            },
            {
                "id": "acc-card-02",
                "type": "CREDIT",
                "subtype": "CREDIT_CARD",
                "name": "GOLD",
                "balance": 1711.19,
                "currencyCode": "BRL",
                "itemId": "item-inter-01",
                "creditData": {
                    "availableCreditLimit": 3000.00,
                    "creditLimit": 5000.00,
                    "balanceDueDate": "2026-08-20"
                }
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
        var options = Options.Create(new PluggyOptions { UserToken = "valid-token", ApiBaseUrl = "https://my-api.pluggy.ai" });
        var client = new MeuPluggyClient(httpClient, options, NullLogger<MeuPluggyClient>.Instance);

        // Act
        var accounts = await client.GetAccountsByItemIdAsync("item-inter-01");

        // Assert
        accounts.Should().HaveCount(2);
        accounts[0].Subtype.Should().Be("CHECKING_ACCOUNT");
        accounts[0].Balance.Should().Be(97.60m);
        accounts[1].Type.Should().Be("CREDIT");
        accounts[1].Balance.Should().Be(1711.19m);
        accounts[1].CreditData?.BalanceDueDate.Should().Be("2026-08-20");
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
            },
            {
                "id": "tx-002",
                "description": "MCDONALDS",
                "amount": 40.00,
                "date": "2026-08-15T00:00:00.000Z",
                "type": "DEBIT",
                "category": "Eating out"
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
        var options = Options.Create(new PluggyOptions { UserToken = "valid-token", ApiBaseUrl = "https://my-api.pluggy.ai" });
        var client = new MeuPluggyClient(httpClient, options, NullLogger<MeuPluggyClient>.Instance);

        // Act
        var txs = await client.GetTransactionsByAccountIdAsync("acc-123");

        // Assert
        txs.Should().HaveCount(2);
        txs[0].Description.Should().Be("Transferência recebida - Fundatec");
        txs[0].Amount.Should().Be(97.60m);
        txs[1].Category.Should().Be("Eating out");
    }
}
