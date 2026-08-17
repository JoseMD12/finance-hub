using System.Net;
using System.Net.Http.Json;
using FinanceHub.UnitTests.Infrastructure;
using FluentAssertions;
using Xunit;

namespace FinanceHub.UnitTests.Api;

[Collection("IntegrationTests")]
public class HealthEndpointsTests
{
    private record HealthResponse(string Status, string Service, string Version);

    [Fact]
    public async Task ApiGateway_GET_Health_ShouldReturnHealthyStatus()
    {
        using var factory = new CustomWebApplicationFactory<FinanceHub.ApiGateway.Program>();
        await factory.InitializeAsync();
        var client = factory.CreateClient();

        try
        {
            var response = await client.GetAsync("/health");
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadFromJsonAsync<HealthResponse>();
            content.Should().NotBeNull();
            content!.Status.Should().Be("Healthy");
            content.Service.Should().Be("FinanceHub.ApiGateway");
        }
        finally
        {
            await factory.DisposeAsync();
        }
    }

    [Fact]
    public async Task AuthConsent_GET_Health_ShouldReturnHealthyStatus()
    {
        using var factory = new CustomWebApplicationFactory<FinanceHub.AuthConsent.Api.Program>();
        await factory.InitializeAsync();
        var client = factory.CreateClient();

        try
        {
            var response = await client.GetAsync("/health");
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadFromJsonAsync<HealthResponse>();
            content.Should().NotBeNull();
            content!.Status.Should().Be("Healthy");
            content.Service.Should().Be("FinanceHub.AuthConsent.Api");
        }
        finally
        {
            await factory.DisposeAsync();
        }
    }



    [Fact]
    public async Task TransactionAggregator_GET_Health_ShouldReturnHealthyStatus()
    {
        using var factory = new CustomWebApplicationFactory<FinanceHub.TransactionAggregator.Api.Program>();
        await factory.InitializeAsync();
        var client = factory.CreateClient();

        try
        {
            var response = await client.GetAsync("/health");
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadFromJsonAsync<HealthResponse>();
            content.Should().NotBeNull();
            content!.Status.Should().Be("Healthy");
            content.Service.Should().Be("FinanceHub.TransactionAggregator.Api");
        }
        finally
        {
            await factory.DisposeAsync();
        }
    }

    [Fact]
    public async Task PluggyIntegration_GET_Health_ShouldReturnHealthyStatus()
    {
        using var factory = new CustomWebApplicationFactory<FinanceHub.PluggyIntegration.Api.Program>();
        await factory.InitializeAsync();
        var client = factory.CreateClient();

        try
        {
            var response = await client.GetAsync("/health");
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadFromJsonAsync<HealthResponse>();
            content.Should().NotBeNull();
            content!.Status.Should().Be("Healthy");
            content.Service.Should().Be("FinanceHub.PluggyIntegration.Api");
        }
        finally
        {
            await factory.DisposeAsync();
        }
    }
}
