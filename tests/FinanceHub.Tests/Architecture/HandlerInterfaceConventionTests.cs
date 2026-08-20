using FluentAssertions;
using NetArchTest.Rules;
using Xunit;

namespace FinanceHub.Tests.Architecture;

[Trait("Category", "Architecture")]
public class HandlerInterfaceConventionTests
{
    private static readonly string[] ApplicationAssemblies =
    [
        "FinanceHub.PluggyIntegration.Application",
        "FinanceHub.TransactionAggregator.Application"
    ];

    [Fact]
    public void Handlers_MustImplementDedicatedInterfaces()
    {
        // Act
        var handlers = Types.InAssemblies(ApplicationAssemblies.Select(System.Reflection.Assembly.Load))
            .That()
            .HaveNameEndingWith("Handler")
            .GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract)
            .ToList();

        var handlersWithoutInterface = handlers
            .Where(h => !h.GetInterfaces().Any(i => i.Name == $"I{h.Name}"))
            .Select(h => h.Name)
            .ToList();

        // Assert
        handlersWithoutInterface.Should().BeEmpty("Regra 13: 100% dos Handlers devem implementar uma interface dedicada com o mesmo nome prefixado por 'I'");
    }
}
