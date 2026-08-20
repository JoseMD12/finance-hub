using FluentAssertions;
using NetArchTest.Rules;
using Xunit;

namespace FinanceHub.Tests.Architecture;

[Trait("Category", "Architecture")]
public class DomainLayerTests
{
    private static readonly string[] DomainAssemblies =
    [
        "FinanceHub.PluggyIntegration.Domain",
        "FinanceHub.TransactionAggregator.Domain"
    ];

    [Fact]
    public void Domain_ShouldNotHaveDependencyOn_Infrastructure_Application_Or_Api()
    {
        // Arrange
        var forbiddenDependencies = new[]
        {
            "FinanceHub.PluggyIntegration.Infrastructure",
            "FinanceHub.PluggyIntegration.Application",
            "FinanceHub.PluggyIntegration.Api",
            "FinanceHub.TransactionAggregator.Infrastructure",
            "FinanceHub.TransactionAggregator.Application",
            "FinanceHub.TransactionAggregator.Api",
            "FinanceHub.ApiGateway",
            "Microsoft.EntityFrameworkCore",
            "Microsoft.AspNetCore"
        };

        // Act
        var result = Types.InAssemblies(DomainAssemblies.Select(System.Reflection.Assembly.Load))
            .ShouldNot()
            .HaveDependencyOnAny(forbiddenDependencies)
            .GetResult();

        // Assert
        result.IsSuccessful.Should().BeTrue("Camada de Domínio não pode depender de Infraestrutura, Aplicação, API ou ORM/EF Core");
    }

    [Fact]
    public void DomainEntities_ShouldBeEncapsulated_AndNotHavePublicSetters()
    {
        // Act
        var types = Types.InAssemblies(DomainAssemblies.Select(System.Reflection.Assembly.Load))
            .That()
            .ResideInNamespaceEndingWith(".Aggregates")
            .Or()
            .ResideInNamespaceEndingWith(".Entities")
            .GetTypes();

        var invalidProperties = types
            .SelectMany(t => t.GetProperties())
            .Where(p => p.SetMethod != null && p.SetMethod.IsPublic)
            .ToList();

        // Assert
        invalidProperties.Should().BeEmpty("Entidades de Domínio devem ter propriedades encapsuladas sem setters públicos (Rich Domain Model)");
    }
}
