using Testcontainers.RabbitMq;
using Xunit;

namespace FinanceHub.UnitTests.Fixtures;

public class RabbitMqTestContainerFixture : IAsyncLifetime
{
    public RabbitMqContainer Container { get; } = new RabbitMqBuilder()
        .WithImage("rabbitmq:3.12-management-alpine")
        .WithUsername("test_guest")
        .WithPassword("test_guest")
        .Build();

    public string Host => Container.Hostname;
    public ushort Port => Container.GetMappedPublicPort(5672);
    public string Username => "test_guest";
    public string Password => "test_guest";

    public async Task InitializeAsync()
    {
        await Container.StartAsync();
    }

    public async Task DisposeAsync()
    {
        await Container.DisposeAsync();
    }
}
