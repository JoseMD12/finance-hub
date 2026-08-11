using FinanceHub.Shared.Messaging.Events;
using FinanceHub.Shared.Messaging.Extensions;
using FluentAssertions;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace FinanceHub.UnitTests.Shared;

public class MessagingTests
{
    [Fact]
    public void TransactionIngested_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var id = Guid.NewGuid();

        // Act
        var evt = new TransactionIngested(
            IngestionId: id,
            Source: "Itau",
            AccountId: "acc-100",
            BankTransactionId: "tx-555",
            Amount: 250.50m,
            TransactionDate: now.Date,
            Description: "Mercado Central",
            Currency: "BRL",
            RawPayloadJson: "{\"id\":\"tx-555\"}",
            OccurredAtUtc: now
        );

        // Assert
        evt.IngestionId.Should().Be(id);
        evt.Source.Should().Be("Itau");
        evt.AccountId.Should().Be("acc-100");
        evt.BankTransactionId.Should().Be("tx-555");
        evt.Amount.Should().Be(250.50m);
        evt.Description.Should().Be("Mercado Central");
        evt.Currency.Should().Be("BRL");
        evt.RawPayloadJson.Should().Be("{\"id\":\"tx-555\"}");
        evt.OccurredAtUtc.Should().Be(now);
    }

    [Fact]
    public void TransactionNormalized_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var txId = Guid.NewGuid();
        var ingestionId = Guid.NewGuid();

        // Act
        var evt = new TransactionNormalized(
            TransactionId: txId,
            IngestionId: ingestionId,
            Source: "MercadoPago",
            AccountId: "acc-200",
            Category: "Alimentacao",
            Amount: 45.00m,
            TransactionDate: now.Date,
            CleanDescription: "Padaria Silva",
            HashDeduplicacao: "sha256hash123",
            ProcessedAtUtc: now
        );

        // Assert
        evt.TransactionId.Should().Be(txId);
        evt.IngestionId.Should().Be(ingestionId);
        evt.Source.Should().Be("MercadoPago");
        evt.Category.Should().Be("Alimentacao");
        evt.Amount.Should().Be(45.00m);
        evt.CleanDescription.Should().Be("Padaria Silva");
        evt.HashDeduplicacao.Should().Be("sha256hash123");
    }

    [Fact]
    public void BankAccountLinked_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var linkId = Guid.NewGuid();

        // Act
        var evt = new BankAccountLinked(
            LinkId: linkId,
            InstitutionId: "itau",
            UserId: "user-777",
            ConsentId: "consent-888",
            LinkedAtUtc: now
        );

        // Assert
        evt.LinkId.Should().Be(linkId);
        evt.InstitutionId.Should().Be("itau");
        evt.UserId.Should().Be("user-777");
        evt.ConsentId.Should().Be("consent-888");
        evt.LinkedAtUtc.Should().Be(now);
    }

    [Fact]
    public void AddFinanceHubMessaging_ShouldRegisterMassTransitServices()
    {
        // Arrange
        var services = new ServiceCollection();
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            { "RabbitMQ:Host", "localhost" },
            { "RabbitMQ:Port", "5672" },
            { "RabbitMQ:Username", "guest" },
            { "RabbitMQ:Password", "guest" }
        }).Build();

        // Act
        services.AddFinanceHubMessaging(config);
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var busControl = serviceProvider.GetService<IBusControl>();
        busControl.Should().NotBeNull();
    }
}
