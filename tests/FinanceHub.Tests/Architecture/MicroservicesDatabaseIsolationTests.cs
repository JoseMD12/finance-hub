using FluentAssertions;
using NetArchTest.Rules;
using Xunit;

namespace FinanceHub.Tests.Architecture;

[Trait("Category", "Architecture")]
public class MicroservicesDatabaseIsolationTests
{
    [Fact]
    public void PluggyIntegration_ShouldNotHaveDependencyOn_TransactionAggregator_DbContext_Or_Repositories()
    {
        // Arrange
        var pluggyAssemblies = new[]
        {
            "FinanceHub.PluggyIntegration.Domain",
            "FinanceHub.PluggyIntegration.Application",
            "FinanceHub.PluggyIntegration.Infrastructure",
            "FinanceHub.PluggyIntegration.Api"
        };

        var aggregatorDependencies = new[]
        {
            "FinanceHub.TransactionAggregator.Infrastructure",
            "FinanceHub.TransactionAggregator.Domain"
        };

        // Act
        var result = Types.InAssemblies(pluggyAssemblies.Select(System.Reflection.Assembly.Load))
            .ShouldNot()
            .HaveDependencyOnAny(aggregatorDependencies)
            .GetResult();

        // Assert
        result.IsSuccessful.Should().BeTrue("Isolamento de Microsserviços: PluggyIntegration não pode acessar os tipos ou banco de dados do TransactionAggregator diretamente");
    }

    [Fact]
    public void TransactionAggregator_ShouldNotHaveDependencyOn_PluggyIntegration_DbContext_Or_Repositories()
    {
        // Arrange
        var aggregatorAssemblies = new[]
        {
            "FinanceHub.TransactionAggregator.Domain",
            "FinanceHub.TransactionAggregator.Application",
            "FinanceHub.TransactionAggregator.Infrastructure",
            "FinanceHub.TransactionAggregator.Api"
        };

        var pluggyDependencies = new[]
        {
            "FinanceHub.PluggyIntegration.Infrastructure",
            "FinanceHub.PluggyIntegration.Domain"
        };

        // Act
        var result = Types.InAssemblies(aggregatorAssemblies.Select(System.Reflection.Assembly.Load))
            .ShouldNot()
            .HaveDependencyOnAny(pluggyDependencies)
            .GetResult();

        // Assert
        result.IsSuccessful.Should().BeTrue("Isolamento de Microsserviços: TransactionAggregator não pode acessar os tipos ou banco de dados do PluggyIntegration diretamente");
    }
}
