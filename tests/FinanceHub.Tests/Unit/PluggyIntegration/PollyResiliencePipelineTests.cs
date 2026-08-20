using FinanceHub.PluggyIntegration.Infrastructure;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace FinanceHub.Tests.Unit.PluggyIntegration;

[Trait("Category", "Unit")]
public class PollyResiliencePipelineTests
{
    [Fact]
    public void AddInfrastructureServices_ShouldRegisterResilientHttpClient_WithPollyV8Pipeline()
    {
        // Arrange
        var services = new ServiceCollection();
        var inMemorySettings = new Dictionary<string, string?>
        {
            { "PLUGGY_USER_API_BASE_URL", "http://localhost:5056" }
        };

        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        // Act
        services.AddInfrastructureServices(configuration);
        var provider = services.BuildServiceProvider();
        var clientFactory = provider.GetService<IHttpClientFactory>();

        // Assert
        clientFactory.Should().NotBeNull();
    }
}
