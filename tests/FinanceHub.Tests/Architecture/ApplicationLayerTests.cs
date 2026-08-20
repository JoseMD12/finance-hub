using FluentAssertions;
using NetArchTest.Rules;
using Xunit;

namespace FinanceHub.Tests.Architecture;

[Trait("Category", "Architecture")]
public class ApplicationLayerTests
{
    private static readonly string[] ApplicationAssemblies =
    [
        "FinanceHub.PluggyIntegration.Application",
        "FinanceHub.TransactionAggregator.Application"
    ];

    [Fact]
    public void Application_ShouldNotDependOn_Infrastructure_Api_Or_EntityFrameworkCore()
    {
        // Arrange
        var forbiddenDependencies = new[]
        {
            "FinanceHub.PluggyIntegration.Infrastructure",
            "FinanceHub.PluggyIntegration.Api",
            "FinanceHub.TransactionAggregator.Infrastructure",
            "FinanceHub.TransactionAggregator.Api",
            "FinanceHub.ApiGateway",
            "Microsoft.EntityFrameworkCore",
            "Npgsql"
        };

        // Act
        var result = Types.InAssemblies(ApplicationAssemblies.Select(System.Reflection.Assembly.Load))
            .ShouldNot()
            .HaveDependencyOnAny(forbiddenDependencies)
            .GetResult();

        // Assert
        result.IsSuccessful.Should().BeTrue("Camada de Aplicação deve depender apenas do Domínio e Abstrações de Interfaces");
    }
}
