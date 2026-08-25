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
    public async Task GetAccountsAsync_WhenDownstreamReturns200_ShouldReturnAccountsAndPropagateHeader()
    {
        // Arrange
        var mockHandler = new MockHttpMessageHandler
        {
            ResponseToReturn = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new List<GatewayPluggyAccountDto>
                {
                    new(
                        ItemId: "item-001",
                        InstitutionName: "Itaú",
                        Name: "Conta Corrente",
                        Type: "BANK",
                        Subtype: "CHECKING_ACCOUNT",
                        Balance: 1500.50m,
                        CreditData: null
                    )
                })
            }
        };

        var httpClient = new HttpClient(mockHandler) { BaseAddress = new Uri("http://localhost:5056") };
        var client = new PluggyIntegrationServiceClient(httpClient, NullLogger<PluggyIntegrationServiceClient>.Instance);

        // Act
        var result = await client.GetAccountsAsync(ValidToken);

        // Assert
        result.Should().NotBeNull().And.HaveCount(1);
        result[0].InstitutionName.Should().Be("Itaú");
        result[0].Balance.Should().Be(1500.50m);

        mockHandler.LastRequest.Should().NotBeNull();
        mockHandler.LastRequest!.Headers.Contains(FinanceHubHeaderNames.PluggyAccessToken).Should().BeTrue();
        mockHandler.LastRequest.Headers.GetValues(FinanceHubHeaderNames.PluggyAccessToken).First().Should().Be(ValidToken);
    }

    [Fact]
    public async Task TriggerSyncAsync_WhenDownstreamReturns202_ShouldReturnSyncJobAcceptedDto()
    {
        // Arrange
        var jobId = Guid.NewGuid();
        var mockHandler = new MockHttpMessageHandler
        {
            ResponseToReturn = new HttpResponseMessage(HttpStatusCode.Accepted)
            {
                Content = JsonContent.Create(new GatewaySyncJobAcceptedDto(
                    JobId: jobId,
                    Status: "Processing",
                    Message: "Sincronização em lote iniciada com sucesso em segundo plano.",
                    StartedAtUtc: DateTime.UtcNow
                ))
            }
        };

        var httpClient = new HttpClient(mockHandler) { BaseAddress = new Uri("http://localhost:5056") };
        var client = new PluggyIntegrationServiceClient(httpClient, NullLogger<PluggyIntegrationServiceClient>.Instance);

        // Act
        var result = await client.TriggerSyncAsync("usr-001", ValidToken);

        // Assert
        result.Should().NotBeNull();
        result!.JobId.Should().Be(jobId);
        result.Status.Should().Be("Processing");

        mockHandler.LastRequest.Should().NotBeNull();
        mockHandler.LastRequest!.Headers.Contains(FinanceHubHeaderNames.PluggyAccessToken).Should().BeTrue();
        mockHandler.LastRequest.Headers.GetValues(FinanceHubHeaderNames.PluggyAccessToken).First().Should().Be(ValidToken);
    }

    [Fact]
    public async Task GetSyncJobStatusAsync_WhenDownstreamReturns200_ShouldReturnSyncJobStatusDto()
    {
        // Arrange
        var jobId = Guid.NewGuid();
        var mockHandler = new MockHttpMessageHandler
        {
            ResponseToReturn = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new GatewaySyncJobStatusDto(
                    JobId: jobId,
                    Status: "Completed",
                    Message: "Sincronização concluída com sucesso.",
                    StartedAtUtc: DateTime.UtcNow.AddSeconds(-3),
                    CompletedAtUtc: DateTime.UtcNow,
                    Result: new GatewayPluggySyncSummaryDto(
                        TotalItemsSynced: 3,
                        TotalAccountsSynced: 6,
                        TotalCheckingTransactionsIngested: 827,
                        TotalCardTransactionsIngested: 599,
                        SyncedAtUtc: DateTime.UtcNow
                    )
                ))
            }
        };

        var httpClient = new HttpClient(mockHandler) { BaseAddress = new Uri("http://localhost:5056") };
        var client = new PluggyIntegrationServiceClient(httpClient, NullLogger<PluggyIntegrationServiceClient>.Instance);

        // Act
        var result = await client.GetSyncJobStatusAsync(jobId);

        // Assert
        result.Should().NotBeNull();
        result!.JobId.Should().Be(jobId);
        result.Status.Should().Be("Completed");
        result.Result.Should().NotBeNull();
        result.Result!.TotalItemsSynced.Should().Be(3);
        result.Result.TotalAccountsSynced.Should().Be(6);
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
