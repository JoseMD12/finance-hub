using System;
using FinanceHub.TransactionAggregator.Domain.Exceptions;
using FinanceHub.TransactionAggregator.Domain.ValueObjects;
using FluentAssertions;
using Xunit;

namespace FinanceHub.UnitTests.TransactionAggregator.Domain;

public class ValueObjectsTests
{
    [Fact]
    public void Money_Creation_WithValidParameters_ShouldSucceed()
    {
        // Arrange & Act
        var money = new Money(150.75m, "BRL");

        // Assert
        money.Amount.Should().Be(150.75m);
        money.Currency.Should().Be("BRL");
    }

    [Fact]
    public void Money_Creation_WithInvalidCurrency_ShouldThrowDomainException()
    {
        // Act
        Action act = () => new Money(100m, "");

        // Assert
        act.Should().Throw<InvalidCurrencyDomainException>()
            .WithMessage("*Moeda obrigatoria*");
    }

    [Fact]
    public void Money_Add_WithSameCurrency_ShouldReturnSum()
    {
        // Arrange
        var m1 = new Money(100m, "BRL");
        var m2 = new Money(50m, "BRL");

        // Act
        var result = m1.Add(m2);

        // Assert
        result.Amount.Should().Be(150m);
        result.Currency.Should().Be("BRL");
    }

    [Fact]
    public void Money_Add_WithDifferentCurrency_ShouldThrowCurrencyMismatchException()
    {
        // Arrange
        var m1 = new Money(100m, "BRL");
        var m2 = new Money(50m, "USD");

        // Act
        Action act = () => m1.Add(m2);

        // Assert
        act.Should().Throw<CurrencyMismatchDomainException>()
            .WithMessage("*Nao e possivel realizar operacoes financeiras entre moedas distintas*");
    }

    [Fact]
    public void TransactionHash_Creation_WithValid64CharHex_ShouldSucceed()
    {
        // Arrange
        var validHash = new string('a', 64);

        // Act
        var hash = new TransactionHash(validHash);

        // Assert
        hash.Value.Should().Be(validHash);
    }

    [Theory]
    [InlineData("short")]
    [InlineData("not-hex-string-with-invalid-characters-zzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzz")]
    [InlineData(null)]
    public void TransactionHash_Creation_WithInvalidValue_ShouldThrowInvalidTransactionHashException(string? invalidHash)
    {
        // Act
        Action act = () => new TransactionHash(invalidHash!);

        // Assert
        act.Should().Throw<InvalidTransactionHashDomainException>();
    }

    [Fact]
    public void SanitizedDescription_Creation_ShouldSanitizeKnownPrefixes()
    {
        // Arrange
        var raw = "PAG*SupermercadoSilva 12/08 SAO PAULO BR";

        // Act
        var sanitized = SanitizedDescription.Create(raw);

        // Assert
        sanitized.OriginalText.Should().Be(raw);
        sanitized.CleanText.Should().NotContain("PAG*");
        sanitized.CleanText.Should().Contain("SupermercadoSilva");
    }

    [Fact]
    public void AccountIdentifier_Creation_WithValidValues_ShouldSucceed()
    {
        // Act
        var account = new AccountIdentifier("itau", "acc-12345");

        // Assert
        account.InstitutionId.Should().Be("itau");
        account.AccountId.Should().Be("acc-12345");
    }
}
