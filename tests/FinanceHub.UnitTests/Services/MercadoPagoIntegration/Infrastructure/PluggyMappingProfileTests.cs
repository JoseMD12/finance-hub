using System.Text.Json;
using FinanceHub.MercadoPagoIntegration.Infrastructure.Mapping;
using FluentAssertions;
using Xunit;

namespace FinanceHub.UnitTests.Services.MercadoPagoIntegration.Infrastructure;

public class PluggyMappingProfileTests
{
    [Fact]
    public void SanitizePii_ShouldMaskCpfCnpjAndEmail()
    {
        // Arrange
        var rawJson = "{\"payer\":{\"taxNumber\":\"123.456.789-00\"},\"email\":\"jose.silva@mercadopago.com\"}";

        // Act
        var sanitized = PluggyMappingProfile.SanitizePii(rawJson);

        // Assert
        sanitized.Should().NotContain("123.456.789-00");
        sanitized.Should().NotContain("jose.silva@mercadopago.com");
        sanitized.Should().Contain("***.***.***-**");
        sanitized.Should().Contain("***@***.***");
    }

    [Fact]
    public void ToBankAccountDto_ShouldMapFieldsCorrectly()
    {
        // Arrange
        var json = """
        {
            "id": "acc-mp-001",
            "name": "Mercado Pago Conta Digital",
            "number": "129126106",
            "currencyCode": "BRL",
            "type": "PAYMENT"
        }
        """;

        using var doc = JsonDocument.Parse(json);

        // Act
        var dto = PluggyMappingProfile.ToBankAccountDto(doc.RootElement);

        // Assert
        dto.AccountId.Should().Be("acc-mp-001");
        dto.BankIdentifier.Should().Be("mercadopago");
        dto.Currency.Should().Be("BRL");
        dto.Nickname.Should().Contain("129126106");
    }

    [Fact]
    public void ToBankTransactionDto_Debit_ShouldReturnNegativeAmount()
    {
        // Arrange
        var json = """
        {
            "id": "tx-pluggy-01",
            "description": "Pix Enviado - Mercado Pago",
            "amount": 50.00,
            "currencyCode": "BRL",
            "date": "2026-08-15T12:00:00.000Z",
            "type": "DEBIT"
        }
        """;

        using var doc = JsonDocument.Parse(json);

        // Act
        var dto = PluggyMappingProfile.ToBankTransactionDto(doc.RootElement, "acc-mp-001");

        // Assert
        dto.TransactionId.Should().Be("tx-pluggy-01");
        dto.AccountId.Should().Be("acc-mp-001");
        dto.Amount.Should().Be(-50.00m);
        dto.CreditDebitIndicator.Should().Be("DBIT");
        dto.TransactionInformation.Should().Be("Pix Enviado - Mercado Pago");
    }

    [Fact]
    public void ToBankTransactionDto_Credit_ShouldReturnPositiveAmount()
    {
        // Arrange
        var json = """
        {
            "id": "tx-pluggy-02",
            "description": "Pix Recebido - Salário",
            "amount": 1500.00,
            "currencyCode": "BRL",
            "date": "2026-08-15T12:00:00.000Z",
            "type": "CREDIT"
        }
        """;

        using var doc = JsonDocument.Parse(json);

        // Act
        var dto = PluggyMappingProfile.ToBankTransactionDto(doc.RootElement, "acc-mp-001");

        // Assert
        dto.TransactionId.Should().Be("tx-pluggy-02");
        dto.Amount.Should().Be(1500.00m);
        dto.CreditDebitIndicator.Should().Be("CRDT");
    }
}
