using System.Net;
using System.Net.Http.Json;

using FinanceHub.ApiGateway.Clients;
using FinanceHub.ApiGateway.DTOs;
using FinanceHub.ApiGateway.Exceptions;
using FinanceHub.UnitTests.Helpers;

using FluentAssertions;

using Microsoft.Extensions.Logging;

using NSubstitute;

using Xunit;

namespace FinanceHub.UnitTests.ApiGateway.Clients;

public class AuthConsentServiceClientTests
{
    private readonly ILogger<AuthConsentServiceClient> _logger = Substitute.For<ILogger<AuthConsentServiceClient>>();

    [Fact]
    public async Task GetConsentsByUserIdAsync_WhenSuccess_ShouldReturnConsentsList()
    {
        // Arrange
        var mockHandler = new MockHttpMessageHandler
        {
            ResponseToReturn = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new[]
                {
                    new GatewayConsentDto(Guid.NewGuid(), "user-123", "itau", "ext-consent-1", "Authorized", DateTime.UtcNow, DateTime.UtcNow.AddDays(30))
                })
            }
        };

        var httpClient = new HttpClient(mockHandler) { BaseAddress = new Uri("http://localhost:5001") };
        var client = new AuthConsentServiceClient(httpClient, _logger);

        // Act
        var result = await client.GetConsentsByUserIdAsync("user-123");

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result.First().UserId.Should().Be("user-123");
    }

    [Fact]
    public async Task GetConsentsByUserIdAsync_WhenDownstreamReturns500_ShouldThrowGatewayDownstreamException()
    {
        // Arrange
        var mockHandler = new MockHttpMessageHandler
        {
            ResponseToReturn = new HttpResponseMessage(HttpStatusCode.InternalServerError)
            {
                Content = new StringContent("Internal Error")
            }
        };

        var httpClient = new HttpClient(mockHandler) { BaseAddress = new Uri("http://localhost:5001") };
        var client = new AuthConsentServiceClient(httpClient, _logger);

        // Act
        var act = async () => await client.GetConsentsByUserIdAsync("user-123");

        // Assert
        await act.Should().ThrowAsync<GatewayDownstreamException>()
            .WithMessage("*AuthConsent*");
    }
}
