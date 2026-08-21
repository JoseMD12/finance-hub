using System.Net;
using System.Net.Http.Json;
using FinanceHub.ApiGateway.Endpoints;
using FinanceHub.Tests.Infrastructure;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace FinanceHub.Tests.Unit.ApiGateway.Endpoints;

[Collection("IntegrationTests")]
public class AuthGatewayEndpointsTests
{
    [Fact]
    public async Task DevToken_WhenEnvironmentIsProduction_ShouldReturnNotFound()
    {
        // Arrange
        using var factory = new CustomWebApplicationFactory<FinanceHub.ApiGateway.Program>();
        await factory.InitializeAsync();

        var prodFactory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Production");
        });

        var client = prodFactory.CreateClient();

        try
        {
            // Act
            var response = await client.PostAsJsonAsync("/api/v1/gateway/auth/dev-token", new DevTokenRequest("usr_test_123"));

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }
        finally
        {
            await factory.DisposeAsync();
        }
    }

    [Fact]
    public async Task DevToken_WhenEnvironmentIsDevelopment_ShouldReturnOkWithToken()
    {
        // Arrange
        using var factory = new CustomWebApplicationFactory<FinanceHub.ApiGateway.Program>();
        await factory.InitializeAsync();

        var devFactory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
        });

        var client = devFactory.CreateClient();

        try
        {
            // Act
            var response = await client.PostAsJsonAsync("/api/v1/gateway/auth/dev-token", new DevTokenRequest("usr_test_123"));

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadFromJsonAsync<DevTokenResponse>();
            content.Should().NotBeNull();
            content!.AccessToken.Should().NotBeNullOrWhiteSpace();
            content.TokenType.Should().Be("Bearer");
            content.ExpiresIn.Should().Be(86400);
        }
        finally
        {
            await factory.DisposeAsync();
        }
    }
}
