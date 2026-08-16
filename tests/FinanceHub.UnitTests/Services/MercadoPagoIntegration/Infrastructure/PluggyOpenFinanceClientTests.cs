using System.Net;
using FinanceHub.MercadoPagoIntegration.Infrastructure.Configuration;
using FinanceHub.MercadoPagoIntegration.Infrastructure.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace FinanceHub.UnitTests.Services.MercadoPagoIntegration.Infrastructure;

public class PluggyOpenFinanceClientTests
{
    private class FakeHttpHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        }
    }

    [Fact]
    public async Task CreateConnectTokenAsync_ShouldThrowNotImplementedException()
    {
        // Arrange
        using var httpClient = new HttpClient(new FakeHttpHandler()) { BaseAddress = new Uri("https://api.pluggy.ai") };
        var options = Options.Create(new OpenFinanceOptions());
        var client = new PluggyOpenFinanceClient(httpClient, options, NullLogger<PluggyOpenFinanceClient>.Instance);

        // Act
        var act = () => client.CreateConnectTokenAsync(null, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotImplementedException>();
    }

    [Fact]
    public async Task GetAccountsByItemAsync_ShouldThrowNotImplementedException()
    {
        // Arrange
        using var httpClient = new HttpClient(new FakeHttpHandler()) { BaseAddress = new Uri("https://api.pluggy.ai") };
        var options = Options.Create(new OpenFinanceOptions());
        var client = new PluggyOpenFinanceClient(httpClient, options, NullLogger<PluggyOpenFinanceClient>.Instance);

        // Act
        var act = () => client.GetAccountsByItemAsync("item-123", CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotImplementedException>();
    }
}
