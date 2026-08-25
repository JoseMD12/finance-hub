using System.Net;
using FinanceHub.PluggyIntegration.Domain.Exceptions;
using FinanceHub.PluggyIntegration.Infrastructure.Clients;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace FinanceHub.Tests.PluggyIntegration;

public class MeuPluggyClientTests
{
    private const string ValidToken = "valid-token-123";

    private class MockHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> handler) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(handler(request));
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
        var executor = new PluggyHttpExecutor(httpClient, NullLogger<PluggyHttpExecutor>.Instance);
        var client = new MeuPluggyClient(executor);

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
        var executor = new PluggyHttpExecutor(httpClient, NullLogger<PluggyHttpExecutor>.Instance);
        var client = new MeuPluggyClient(executor);

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
        var executor = new PluggyHttpExecutor(httpClient, NullLogger<PluggyHttpExecutor>.Instance);
        var client = new MeuPluggyClient(executor);

        // Act
        var act = async () => await client.GetItemsAsync(ValidToken);

        // Assert
        await act.Should().ThrowAsync<PluggySessionExpiredDomainException>();
    }

    [Fact]
    public async Task GetItemsAsync_WhenApiReturns429_ShouldThrowPluggyRateLimitDomainException()
    {
        // Arrange
        var handler = new MockHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.TooManyRequests));
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://my-api.pluggy.ai") };
        var executor = new PluggyHttpExecutor(httpClient, NullLogger<PluggyHttpExecutor>.Instance);
        var client = new MeuPluggyClient(executor);

        // Act
        var act = async () => await client.GetItemsAsync(ValidToken);

        // Assert
        await act.Should().ThrowAsync<PluggyRateLimitDomainException>();
    }

    [Fact]
    public async Task GetItemsAsync_WhenApiReturns500_ShouldThrowPluggyApiCommunicationDomainException()
    {
        // Arrange
        var handler = new MockHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError)
        {
            Content = new StringContent("Server error", System.Text.Encoding.UTF8, "text/plain")
        });
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://my-api.pluggy.ai") };
        var executor = new PluggyHttpExecutor(httpClient, NullLogger<PluggyHttpExecutor>.Instance);
        var client = new MeuPluggyClient(executor);

        // Act
        var act = async () => await client.GetItemsAsync(ValidToken);

        // Assert
        await act.Should().ThrowAsync<PluggyApiCommunicationDomainException>();
    }

    [Fact]
    public async Task GetAsync_WhenErrorBodyExceeds500Characters_ShouldTruncateInLogger()
    {
        // Arrange
        var hugeError = new string('A', 600);
        var handler = new MockHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError)
        {
            Content = new StringContent(hugeError, System.Text.Encoding.UTF8, "text/plain")
        });
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://my-api.pluggy.ai") };
        var logger = Substitute.For<ILogger<PluggyHttpExecutor>>();
        var executor = new PluggyHttpExecutor(httpClient, logger);

        // Act
        var act = async () => await executor.GetAsync<object>("/test", ValidToken);

        // Assert
        await act.Should().ThrowAsync<PluggyApiCommunicationDomainException>();

        var expectedTruncated = new string('A', 500) + "...";
        logger.Received(1).Log(
            LogLevel.Error,
            Arg.Any<EventId>(),
            Arg.Is<object>(v => v.ToString()!.Contains(expectedTruncated) && !v.ToString()!.Contains(hugeError)),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>()
        );
    }
}
