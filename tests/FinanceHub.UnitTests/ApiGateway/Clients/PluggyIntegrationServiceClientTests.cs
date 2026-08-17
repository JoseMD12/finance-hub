using System.Net;
using System.Net.Http.Json;
using FinanceHub.ApiGateway.Clients;
using FinanceHub.ApiGateway.DTOs;
using FinanceHub.UnitTests.Helpers;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace FinanceHub.UnitTests.ApiGateway.Clients;

public class PluggyIntegrationServiceClientTests
{
    [Fact]
    public async Task TriggerSyncAsync_WhenDownstreamReturns200_ShouldReturnSummary()
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
        var result = await client.TriggerSyncAsync();

        // Assert
        result.Should().NotBeNull();
        result!.TotalItemsSynced.Should().Be(3);
        result.TotalAccountsSynced.Should().Be(6);
        result.TotalCheckingTransactionsIngested.Should().Be(827);
        result.TotalCardTransactionsIngested.Should().Be(599);
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
        var act = async () => await client.TriggerSyncAsync();

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
