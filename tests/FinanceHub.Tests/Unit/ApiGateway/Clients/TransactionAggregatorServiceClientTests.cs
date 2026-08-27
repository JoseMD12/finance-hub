using System.Net;
using System.Net.Http.Json;

using FinanceHub.ApiGateway.Clients;
using FinanceHub.ApiGateway.DTOs;
using FinanceHub.ApiGateway.Exceptions;
using FinanceHub.Tests.Helpers;

using FluentAssertions;

using Microsoft.Extensions.Logging;

using NSubstitute;

using Xunit;

namespace FinanceHub.Tests.ApiGateway.Clients;

public class TransactionAggregatorServiceClientTests
{
    private readonly ILogger<TransactionAggregatorServiceClient> _logger = Substitute.For<ILogger<TransactionAggregatorServiceClient>>();

    [Fact]
    public async Task GetConsolidatedBalanceAsync_WhenSuccess_ShouldReturnConsolidatedBalanceDto()
    {
        // Arrange
        var expectedBalance = new GatewayConsolidatedBalanceDto("user-123", 1500.50m, new[]
        {
            new GatewayAccountBalanceDto("itau", "acc-001", 1500.50m, "BRL", DateTime.UtcNow)
        });

        var mockHandler = new MockHttpMessageHandler
        {
            ResponseToReturn = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(expectedBalance)
            }
        };

        var httpClient = new HttpClient(mockHandler) { BaseAddress = new Uri("http://localhost:5002") };
        var client = new TransactionAggregatorServiceClient(httpClient, _logger);

        // Act
        var result = await client.GetConsolidatedBalanceAsync("user-123");

        // Assert
        result.Should().NotBeNull();
        result.UserId.Should().Be("user-123");
        result.TotalBalanceBrl.Should().Be(1500.50m);
    }

    [Fact]
    public async Task GetConsolidatedBalanceAsync_WhenDownstreamError_ShouldThrowGatewayDownstreamException()
    {
        // Arrange
        var mockHandler = new MockHttpMessageHandler
        {
            ResponseToReturn = new HttpResponseMessage(HttpStatusCode.BadGateway)
            {
                Content = new StringContent("Downstream unavailable")
            }
        };

        var httpClient = new HttpClient(mockHandler) { BaseAddress = new Uri("http://localhost:5002") };
        var client = new TransactionAggregatorServiceClient(httpClient, _logger);

        // Act
        var act = async () => await client.GetConsolidatedBalanceAsync("user-123");

        // Assert
        await act.Should().ThrowAsync<GatewayDownstreamException>()
            .WithMessage("*TransactionAggregator*");
    }

    [Fact]
    public async Task GetTransactionsAsync_WithChannelGroup_ShouldIncludeChannelGroupInQueryString()
    {
        // Arrange
        HttpRequestMessage? capturedRequest = null;
        var mockResponse = new PagedGatewayTransactionsDto(
            Enumerable.Empty<GatewayTransactionDto>(),
            new GatewayTransactionSummaryDto(0m, 0m, 0m, 0),
            1,
            20,
            0,
            0);

        var mockHandler = new MockHttpMessageHandler
        {
            ResponseToReturn = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(mockResponse)
            }
        };

        var httpClient = new HttpClient(mockHandler) { BaseAddress = new Uri("http://localhost:5002") };
        var client = new TransactionAggregatorServiceClient(httpClient, _logger);
        var filter = new GatewayTransactionFilterDto("user-123", 1, 20, ChannelGroup: "credit");

        // Act
        var result = await client.GetTransactionsAsync(filter);

        // Assert
        result.Should().NotBeNull();
        mockHandler.LastRequest.Should().NotBeNull();
        mockHandler.LastRequest!.RequestUri.Should().NotBeNull();
        mockHandler.LastRequest!.RequestUri!.Query.Should().Contain("channelGroup=credit");
    }
}
