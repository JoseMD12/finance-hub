using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace FinanceHub.UnitTests.Api;

public class HealthEndpointsTests
{
    private record HealthResponse(string Status, string Service, string Version);

    [Fact]
    public async Task ApiGateway_GET_Health_ShouldReturnHealthyStatus()
    {
        // Arrange
        using var factory = new WebApplicationFactory<FinanceHub.ApiGateway.Program>();
        var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync("/health");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<HealthResponse>();
        content.Should().NotBeNull();
        content!.Status.Should().Be("Healthy");
        content.Service.Should().Be("FinanceHub.ApiGateway");
    }

    [Fact]
    public async Task AuthConsent_GET_Health_ShouldReturnHealthyStatus()
    {
        // Arrange
        using var factory = new WebApplicationFactory<FinanceHub.AuthConsent.Api.Program>();
        var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync("/health");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<HealthResponse>();
        content.Should().NotBeNull();
        content!.Status.Should().Be("Healthy");
        content.Service.Should().Be("FinanceHub.AuthConsent.Api");
    }

    [Fact]
    public async Task ItauIntegration_GET_Health_ShouldReturnHealthyStatus()
    {
        // Arrange
        using var factory = new WebApplicationFactory<FinanceHub.ItauIntegration.Api.Program>();
        var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync("/health");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<HealthResponse>();
        content.Should().NotBeNull();
        content!.Status.Should().Be("Healthy");
        content.Service.Should().Be("FinanceHub.ItauIntegration.Api");
    }

    [Fact]
    public async Task MercadoPagoIntegration_GET_Health_ShouldReturnHealthyStatus()
    {
        // Arrange
        using var factory = new WebApplicationFactory<FinanceHub.MercadoPagoIntegration.Api.Program>();
        var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync("/health");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<HealthResponse>();
        content.Should().NotBeNull();
        content!.Status.Should().Be("Healthy");
        content.Service.Should().Be("FinanceHub.MercadoPagoIntegration.Api");
    }

    [Fact]
    public async Task TransactionAggregator_GET_Health_ShouldReturnHealthyStatus()
    {
        // Arrange
        using var factory = new WebApplicationFactory<FinanceHub.TransactionAggregator.Api.Program>();
        var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync("/health");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<HealthResponse>();
        content.Should().NotBeNull();
        content!.Status.Should().Be("Healthy");
        content.Service.Should().Be("FinanceHub.TransactionAggregator.Api");
    }
}
