using System;
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

        // Act
        var evt = new TransactionNormalized(
            TransactionId: txId,
            Source: "MercadoPago",
            AccountId: "acc-200",
            Amount: 45.00m,
            Currency: "BRL",
            TransactionType: "Debit",
            TransactionDate: now.Date,
            CleanDescription: "Padaria Silva",
            HashDeduplicacao: "sha256hash123",
            ProcessedAtUtc: now
        );

        // Assert
        evt.TransactionId.Should().Be(txId);
        evt.Source.Should().Be("MercadoPago");
        evt.Amount.Should().Be(45.00m);
        evt.Currency.Should().Be("BRL");
        evt.TransactionType.Should().Be("Debit");
        evt.CleanDescription.Should().Be("Padaria Silva");
        evt.HashDeduplicacao.Should().Be("sha256hash123");
    }

    [Fact]
    public void BankTransactionNormalized_ShouldSetPropertiesAndInheritFromTransactionNormalized()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var txId = Guid.NewGuid();
        var ingestionId = Guid.NewGuid();

        // Act
        var evt = new BankTransactionNormalized(
            TransactionId: txId,
            Source: "Itau",
            AccountId: "acc-100",
            Amount: 100.00m,
            Currency: "BRL",
            TransactionType: "Credit",
            TransactionDate: now.Date,
            CleanDescription: "Pix Recebido",
            HashDeduplicacao: "sha256hash456",
            ProcessedAtUtc: now,
            IngestionId: ingestionId,
            RawPayloadJson: "{\"raw\":true}"
        );

        // Assert
        evt.Should().BeAssignableTo<TransactionNormalized>();
        evt.TransactionId.Should().Be(txId);
        evt.IngestionId.Should().Be(ingestionId);
        evt.RawPayloadJson.Should().Be("{\"raw\":true}");
    }

    [Fact]
    public void TransactionCategorized_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var txId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();

        // Act
        var evt = new TransactionCategorized(
            TransactionId: txId,
            CategoryId: categoryId,
            CategoryName: "Alimentacao",
            CategorizationSource: "UserRule",
            CategorizedAtUtc: now
        );

        // Assert
        evt.TransactionId.Should().Be(txId);
        evt.CategoryId.Should().Be(categoryId);
        evt.CategoryName.Should().Be("Alimentacao");
        evt.CategorizationSource.Should().Be("UserRule");
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
}
