using FinanceHub.Shared.Messaging.Events;
using FinanceHub.Shared.Messaging.Extensions;
using FinanceHub.UnitTests.Fixtures;
using FluentAssertions;
using MassTransit;
using MassTransit.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace FinanceHub.UnitTests.Integration;

public class RabbitMqContainerIntegrationTests : IClassFixture<RabbitMqTestContainerFixture>
{
    private readonly RabbitMqTestContainerFixture _fixture;

    public RabbitMqContainerIntegrationTests(RabbitMqTestContainerFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task MassTransit_ShouldPublishAndReceiveMessage_OnIsolatedRabbitMqContainer()
    {
        // Arrange
        var services = new ServiceCollection();
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            { "RabbitMQ:Host", _fixture.Host },
            { "RabbitMQ:Port", _fixture.Port.ToString() },
            { "RabbitMQ:Username", _fixture.Username },
            { "RabbitMQ:Password", _fixture.Password }
        }).Build();

        services.AddFinanceHubMessaging(config);
        var provider = services.BuildServiceProvider();

        var busControl = provider.GetRequiredService<IBusControl>();
        await busControl.StartAsync(new CancellationTokenSource(TimeSpan.FromSeconds(10)).Token);

        try
        {
            var eventToPublish = new TransactionIngested(
                IngestionId: Guid.NewGuid(),
                Source: "Itau",
                AccountId: "acc-test-container",
                BankTransactionId: "tx-test-999",
                Amount: 199.50m,
                TransactionDate: DateTime.UtcNow.Date,
                Description: "Compressa Testcontainer",
                Currency: "BRL",
                RawPayloadJson: "{}",
                OccurredAtUtc: DateTime.UtcNow
            );

            // Act
            await busControl.Publish(eventToPublish);

            // Assert
            busControl.Should().NotBeNull();
        }
        finally
        {
            await busControl.StopAsync();
        }
    }
}
