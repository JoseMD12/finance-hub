using System.Net;
using System.Net.Http.Json;
using FinanceHub.ApiGateway.Clients;
using FinanceHub.ApiGateway.DTOs;
using FinanceHub.Shared.Messaging.Constants;
using FinanceHub.Tests.Helpers;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace FinanceHub.Tests.ApiGateway.Clients;

public class PluggyIntegrationServiceClientTests
{
    private const string ValidToken = "pluggy-token-123";

    [Fact]
    public async Task TriggerSyncAsync_WhenDownstreamReturns200_ShouldReturnSummaryAndPropagateHeader()
    {
        // Arrange
        var mockHandler = new MockHttpMessageHandler
        {
            ResponseToReturn = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new GatewayPluggySyncSummaryDto(
                    TotalItemsSynced: 3,
                    TotalAccountsSynced: 6,
                    TotalCheckingTransactionsIngested: 827,
                    TotalCardTransactionsIngested: 599,
                    SyncedAtUtc: DateTime.UtcNow
                ))
            }
        };

        var httpClient = new HttpClient(mockHandler) { BaseAddress = new Uri("http://localhost:5056") };
        var client = new PluggyIntegrationServiceClient(httpClient, NullLogger<PluggyIntegrationServiceClient>.Instance);

        // Act
        var result = await client.TriggerSyncAsync("usr-001", ValidToken);

        // Assert
        result.Should().NotBeNull();
        result!.TotalItemsSynced.Should().Be(3);
        result.TotalAccountsSynced.Should().Be(6);
        result.TotalCheckingTransactionsIngested.Should().Be(827);
        result.TotalCardTransactionsIngested.Should().Be(599);

        mockHandler.LastRequest.Should().NotBeNull();
        mockHandler.LastRequest!.Headers.Contains(FinanceHubHeaderNames.PluggyAccessToken).Should().BeTrue();
        mockHandler.LastRequest.Headers.GetValues(FinanceHubHeaderNames.PluggyAccessToken).First().Should().Be(ValidToken);
    }

    [Fact]
    public async Task TriggerSyncAsync_WhenDownstreamReturns500_ShouldThrowHttpRequestException()
    {
        // Arrange
        var mockHandler = new MockHttpMessageHandler
        {
            ResponseToReturn = new HttpResponseMessage(HttpStatusCode.InternalServerError)
        };

        var httpClient = new HttpClient(mockHandler) { BaseAddress = new Uri("http://localhost:5056") };
        var client = new PluggyIntegrationServiceClient(httpClient, NullLogger<PluggyIntegrationServiceClient>.Instance);

        // Act
        var act = async () => await client.TriggerSyncAsync("usr-001", ValidToken);

        // Assert
        await act.Should().ThrowAsync<HttpRequestException>();
    }

    [Fact]
    public async Task HealthCheckAsync_WhenDownstreamReturns200_ShouldReturnTrue()
    {
        // Arrange
        var mockHandler = new MockHttpMessageHandler
        {
            ResponseToReturn = new HttpResponseMessage(HttpStatusCode.OK)
        };

        var httpClient = new HttpClient(mockHandler) { BaseAddress = new Uri("http://localhost:5056") };
        var client = new PluggyIntegrationServiceClient(httpClient, NullLogger<PluggyIntegrationServiceClient>.Instance);

        // Act
        var result = await client.HealthCheckAsync();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task HealthCheckAsync_WhenDownstreamReturns500_ShouldReturnFalse()
    {
        // Arrange
        var mockHandler = new MockHttpMessageHandler
        {
            ResponseToReturn = new HttpResponseMessage(HttpStatusCode.InternalServerError)
        };

        var httpClient = new HttpClient(mockHandler) { BaseAddress = new Uri("http://localhost:5056") };
        var client = new PluggyIntegrationServiceClient(httpClient, NullLogger<PluggyIntegrationServiceClient>.Instance);

        // Act
        var result = await client.HealthCheckAsync();

        // Assert
        result.Should().BeFalse();
    }
}
