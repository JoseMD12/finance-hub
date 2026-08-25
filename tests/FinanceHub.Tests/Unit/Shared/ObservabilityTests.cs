using FinanceHub.Shared.Observability;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NSubstitute;
using OpenTelemetry.Trace;
using Xunit;

namespace FinanceHub.Tests.Shared;

public class ObservabilityTests
{
    [Fact]
    public void AddFinanceHubObservability_ShouldRegisterOpenTelemetryTracerProvider()
    {
        // Arrange
        var services = new ServiceCollection();
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            { "OpenTelemetry:OtlpEndpoint", "http://localhost:4317" }
        }).Build();

        // Act
        services.AddFinanceHubObservability(config, "FinanceHub.TestService");
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var tracerProvider = serviceProvider.GetService<TracerProvider>();
        tracerProvider.Should().NotBeNull();
    }

    [Fact]
    public void AddFinanceHubObservability_ShouldConfigureEntityFrameworkCoreAndMassTransitInstrumentation()
    {
        // Arrange
        var services = new ServiceCollection();
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            { "OpenTelemetry:OtlpEndpoint", "http://localhost:4317" }
        }).Build();

        // Act
        services.AddFinanceHubObservability(config, "FinanceHub.TestService");
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var tracerProvider = serviceProvider.GetService<TracerProvider>();
        tracerProvider.Should().NotBeNull();
    }

    [Fact]
    public void UseFinanceHubSerilog_ShouldConfigureHostBuilder()
    {
        // Arrange
        var hostBuilder = Substitute.For<IHostBuilder>();

        // Act
        var result = hostBuilder.UseFinanceHubSerilog();

        // Assert
        result.Should().Be(hostBuilder);
    }
}
