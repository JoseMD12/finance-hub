using System;
using FinanceHub.TransactionAggregator.Domain.Entities;
using FinanceHub.TransactionAggregator.Domain.Exceptions;
using FinanceHub.TransactionAggregator.Domain.ValueObjects;
using FluentAssertions;
using Xunit;

namespace FinanceHub.UnitTests.TransactionAggregator.Domain;

public class EntitiesTests
{
    [Fact]
    public void CanonicalTransaction_Creation_WithValidParameters_ShouldSucceed()
    {
        // Arrange
        var accountInfo = new AccountIdentifier("itau", "acc-99");
        var hash = new TransactionHash(new string('b', 64));
        var amount = new Money(250.50m, "BRL");
        var description = SanitizedDescription.Create("PAG*Restaurante 12/08");
        var bankDetails = new BankTransactionDetails("bank-tx-123", TransactionChannel.Pix, "Restaurante");

        // Act
        var creationParams = new CanonicalTransactionCreationParams(
            "user-777",
            accountInfo,
            hash,
            amount,
            TransactionType.Debit,
            description,
            Guid.NewGuid(),
            CategorizationSource.GlobalRule,
            DateTime.UtcNow,
            bankDetails);

        var transaction = CanonicalTransaction.Create(creationParams);

        // Assert
        transaction.Id.Should().NotBeEmpty();
        transaction.UserId.Should().Be("user-777");
        transaction.AccountInfo.Should().Be(accountInfo);
        transaction.Hash.Should().Be(hash);
        transaction.Amount.Should().Be(amount);
        transaction.Type.Should().Be(TransactionType.Debit);
        transaction.IsManuallyCategorized.Should().BeFalse();
    }

    [Fact]
    public void CanonicalTransaction_CategorizeManually_ShouldSetFlagAndCategorizationSource()
    {
        // Arrange
        var transaction = CreateSampleTransaction();
        var newCategoryId = Guid.NewGuid();

        // Act
        transaction.CategorizeManually(newCategoryId);

        // Assert
        transaction.CategoryId.Should().Be(newCategoryId);
        transaction.CategorizationSource.Should().Be(CategorizationSource.UserManual);
        transaction.IsManuallyCategorized.Should().BeTrue();
    }

    [Fact]
    public void AccountBalance_ApplyTransaction_Credit_ShouldIncreaseBalance()
    {
        // Arrange
        var accountInfo = new AccountIdentifier("itau", "acc-100");
        var initialMoney = new Money(500m, "BRL");
        var balance = AccountBalance.Create("user-1", accountInfo, initialMoney);

        // Act
        balance.ApplyTransaction(new Money(200m, "BRL"), TransactionType.Credit);

        // Assert
        balance.CurrentBalance.Amount.Should().Be(700m);
    }

    [Fact]
    public void AccountBalance_ApplyTransaction_Debit_ShouldDecreaseBalance()
    {
        // Arrange
        var accountInfo = new AccountIdentifier("itau", "acc-100");
        var initialMoney = new Money(500m, "BRL");
        var balance = AccountBalance.Create("user-1", accountInfo, initialMoney);

        // Act
        balance.ApplyTransaction(new Money(150m, "BRL"), TransactionType.Debit);

        // Assert
        balance.CurrentBalance.Amount.Should().Be(350m);
    }

    private static CanonicalTransaction CreateSampleTransaction()
    {
        var creationParams = new CanonicalTransactionCreationParams(
            "user-1",
            new AccountIdentifier("itau", "acc-1"),
            new TransactionHash(new string('c', 64)),
            new Money(100m, "BRL"),
            TransactionType.Credit,
            SanitizedDescription.Create("Depósito PIX"),
            Guid.NewGuid(),
            CategorizationSource.Fallback,
            DateTime.UtcNow,
            new BankTransactionDetails("tx-1", TransactionChannel.Pix, "Pagador"));

        return CanonicalTransaction.Create(creationParams);
    }
}
