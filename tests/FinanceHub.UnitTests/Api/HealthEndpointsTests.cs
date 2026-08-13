using System.Net;
using System.Net.Http.Json;
using FinanceHub.UnitTests.Fixtures;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace FinanceHub.UnitTests.Api;

public class CustomWebApplicationFactory<TProgram> : WebApplicationFactory<TProgram>, IAsyncLifetime
    where TProgram : class
{
    private readonly PostgreSqlTestContainerFixture _postgresFixture = new();
    private readonly RabbitMqTestContainerFixture _rabbitMqFixture = new();

    public async Task InitializeAsync()
    {
        await _postgresFixture.InitializeAsync();
        await _rabbitMqFixture.InitializeAsync();
    }

    public new async Task DisposeAsync()
    {
        await _postgresFixture.DisposeAsync();
        await _rabbitMqFixture.DisposeAsync();
        await base.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:AuthConsentDb", _postgresFixture.ConnectionString);
        builder.UseSetting("ConnectionStrings:TransactionAggregatorDb", _postgresFixture.ConnectionString);
        builder.UseSetting("ConnectionStrings:DefaultConnection", _postgresFixture.ConnectionString);
        builder.UseSetting("RabbitMQ:Host", _rabbitMqFixture.Host);
        builder.UseSetting("RabbitMQ:Port", _rabbitMqFixture.Port.ToString());
        builder.UseSetting("RabbitMQ:Username", _rabbitMqFixture.Username);
        builder.UseSetting("RabbitMQ:Password", _rabbitMqFixture.Password);

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "ConnectionStrings:AuthConsentDb", _postgresFixture.ConnectionString },
                { "ConnectionStrings:TransactionAggregatorDb", _postgresFixture.ConnectionString },
                { "ConnectionStrings:DefaultConnection", _postgresFixture.ConnectionString },
                { "RabbitMQ:Host", _rabbitMqFixture.Host },
                { "RabbitMQ:Port", _rabbitMqFixture.Port.ToString() },
                { "RabbitMQ:Username", _rabbitMqFixture.Username },
                { "RabbitMQ:Password", _rabbitMqFixture.Password }
            });
        });
    }
}

[CollectionDefinition("IntegrationTests", DisableParallelization = true)]
public class IntegrationTestCollection : ICollectionFixture<PostgreSqlTestContainerFixture>
{
}

[Collection("IntegrationTests")]
public class HealthEndpointsTests
{
    private record HealthResponse(string Status, string Service, string Version);

    [Fact]
    public async Task ApiGateway_GET_Health_ShouldReturnHealthyStatus()
    {
        // Arrange
        using var factory = new CustomWebApplicationFactory<FinanceHub.ApiGateway.Program>();
        await factory.InitializeAsync();
        var client = factory.CreateClient();

        try
        {
            // Act
            var response = await client.GetAsync("/health");

            // Assert
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
        // Arrange
        using var factory = new CustomWebApplicationFactory<FinanceHub.AuthConsent.Api.Program>();
        await factory.InitializeAsync();
        var client = factory.CreateClient();

        try
        {
            // Act
            var response = await client.GetAsync("/health");

            // Assert
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
    public async Task ItauIntegration_GET_Health_ShouldReturnHealthyStatus()
    {
        // Arrange
        using var factory = new CustomWebApplicationFactory<FinanceHub.ItauIntegration.Api.Program>();
        await factory.InitializeAsync();
        var client = factory.CreateClient();

        try
        {
            // Act
            var response = await client.GetAsync("/health");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadFromJsonAsync<HealthResponse>();
            content.Should().NotBeNull();
            content!.Status.Should().Be("Healthy");
            content.Service.Should().Be("FinanceHub.ItauIntegration.Api");
        }
        finally
        {
            await factory.DisposeAsync();
        }
    }

    [Fact]
    public async Task MercadoPagoIntegration_GET_Health_ShouldReturnHealthyStatus()
    {
        // Arrange
        using var factory = new CustomWebApplicationFactory<FinanceHub.MercadoPagoIntegration.Api.Program>();
        await factory.InitializeAsync();
        var client = factory.CreateClient();

        try
        {
            // Act
            var response = await client.GetAsync("/health");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadFromJsonAsync<HealthResponse>();
            content.Should().NotBeNull();
            content!.Status.Should().Be("Healthy");
            content.Service.Should().Be("FinanceHub.MercadoPagoIntegration.Api");
        }
        finally
        {
            await factory.DisposeAsync();
        }
    }

    [Fact]
    public async Task TransactionAggregator_GET_Health_ShouldReturnHealthyStatus()
    {
        // Arrange
        using var factory = new CustomWebApplicationFactory<FinanceHub.TransactionAggregator.Api.Program>();
        await factory.InitializeAsync();
        var client = factory.CreateClient();

        try
        {
            // Act
            var response = await client.GetAsync("/health");

            // Assert
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
}
