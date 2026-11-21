using System;
using System.Collections.Generic;
using System.Text.Json;
using FinanceHub.PluggyIntegration.Application.DTOs;
using FinanceHub.PluggyIntegration.Application.Services;
using FinanceHub.PluggyIntegration.Domain.Entities;
using FinanceHub.Shared.Messaging.Events;
using FluentAssertions;
using Xunit;

namespace FinanceHub.Tests.Services.PluggyIntegration;

/// <summary>
/// Cobre o mapeamento dos dados de crédito da Meu.Pluggy. Os casos reproduzem o formato do payload
/// observado em 27/08/2026 sobre as conexões Itaú, Banco Inter e Mercado Pago — inclusive as
/// duas surpresas: <c>balanceCloseDate</c> sempre nulo e <c>billForecastDate</c> ausente em
/// cerca de um terço das transações.
/// </summary>
public class PluggyCreditDataMappingTests
{
    private readonly PluggyTransactionMapper _mapper = new();

    [Fact]
    public void MapTransactionToEvents_WhenInstallmentPurchase_ShouldCarryInstallmentsToEvent()
    {
        var cardEvents = new List<InvoiceItemIngested>();

        _mapper.MapTransactionToEvents(
            BuildTransaction(installmentNumber: 2, totalInstallments: 6),
            BuildCreditAccount(),
            "Itaú",
            "user-1",
            new List<TransactionIngested>(),
            cardEvents);

        cardEvents.Should().ContainSingle();
        cardEvents[0].CurrentInstallment.Should().Be(2);
        cardEvents[0].TotalInstallments.Should().Be(6);
    }

    [Fact]
    public void MapTransactionToEvents_ShouldCarryInvoiceDueDateToEvent()
    {
        var cardEvents = new List<InvoiceItemIngested>();

        _mapper.MapTransactionToEvents(
            BuildTransaction(),
            BuildCreditAccount(balanceDueDate: "2026-08-25"),
            "Itaú",
            "user-1",
            new List<TransactionIngested>(),
            cardEvents);

        cardEvents[0].InvoiceDueDate.Should().Be(new DateTime(2026, 8, 25, 0, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public void MapTransactionToEvents_WhenSinglePayment_ShouldNotReportInstallments()
    {
        // A Meu.Pluggy omite creditCardMetadata.totalInstallments em compras à vista.
        var cardEvents = new List<InvoiceItemIngested>();

        _mapper.MapTransactionToEvents(
            BuildTransaction(installmentNumber: null, totalInstallments: null),
            BuildCreditAccount(),
            "Itaú",
            "user-1",
            new List<TransactionIngested>(),
            cardEvents);

        cardEvents[0].CurrentInstallment.Should().BeNull();
        cardEvents[0].TotalInstallments.Should().BeNull();
    }

    [Fact]
    public void MapTransactionToEvents_WhenConnectorReportsZeroInstallments_ShouldNormalizeToNull()
    {
        // Zero parcelas é ruído de connector, não parcelamento válido: precisa virar null antes
        // de chegar no Aggregator, cujo domínio rejeita numeração de parcela não positiva.
        var cardEvents = new List<InvoiceItemIngested>();

        _mapper.MapTransactionToEvents(
            BuildTransaction(installmentNumber: 0, totalInstallments: 0),
            BuildCreditAccount(),
            "Itaú",
            "user-1",
            new List<TransactionIngested>(),
            cardEvents);

        cardEvents[0].CurrentInstallment.Should().BeNull();
        cardEvents[0].TotalInstallments.Should().BeNull();
    }

    [Fact]
    public void MapTransactionToEvents_WhenCheckingAccount_ShouldEmitCheckingEventWithoutInvoiceData()
    {
        var checkingEvents = new List<TransactionIngested>();
        var cardEvents = new List<InvoiceItemIngested>();

        _mapper.MapTransactionToEvents(
            BuildTransaction(),
            BuildCheckingAccount(),
            "Banco Inter",
            "user-1",
            checkingEvents,
            cardEvents);

        checkingEvents.Should().ContainSingle();
        cardEvents.Should().BeEmpty();
    }

    // ─── Desserialização do contrato real ──────────────────────────────────────

    [Fact]
    public void CreditDataDto_ShouldDeserializeRealPayloadAndCaptureUnmappedFields()
    {
        // Payload no formato devolvido pela API; os valores são fictícios.
        const string json = """
        {
          "level": "GOLD",
          "brand": "MASTERCARD",
          "balanceCloseDate": null,
          "balanceDueDate": "2026-08-25",
          "availableCreditLimit": 8000.00,
          "minimumPayment": 200.00,
          "creditLimit": 10000,
          "status": "ACTIVE"
        }
        """;

        var dto = JsonSerializer.Deserialize<PluggyCreditDataDto>(
            json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;

        dto.CreditLimit.Should().Be(10000m);
        dto.AvailableCreditLimit.Should().Be(8000.00m);
        dto.BalanceDueDate.Should().Be("2026-08-25");
        dto.MinimumPayment.Should().Be(200.00m);

        // Confirmado empiricamente: o campo existe no contrato, mas vem nulo.
        dto.BalanceCloseDate.Should().BeNull();

        // Campos que ainda não mapeamos ficam capturados para descoberta futura.
        dto.AdditionalFields.Should().ContainKeys("level", "brand", "status");
    }

    [Fact]
    public void CreditCardMetadataDto_ShouldDeserializeBillForecastDateAndPurchaseDate()
    {
        const string json = """
        {
          "cardNumber": "0000",
          "payeeMCC": 5411,
          "billForecastDate": "2026-08",
          "installmentNumber": 2,
          "totalInstallments": 2,
          "purchaseDate": "2026-07-06T00:00:00.000Z",
          "billId": "00000000-0000-4000-8000-000000000001"
        }
        """;

        var dto = JsonSerializer.Deserialize<PluggyCreditCardMetadataDto>(
            json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;

        dto.BillForecastDate.Should().Be("2026-08");
        dto.BillId.Should().Be("00000000-0000-4000-8000-000000000001");
        dto.InstallmentNumber.Should().Be(2);
        dto.TotalInstallments.Should().Be(2);

        // Parcela lançada em agosto, mas comprada em julho: as duas datas são diferentes.
        dto.PurchaseDate.Should().Be(new DateTime(2026, 7, 6, 0, 0, 0, DateTimeKind.Utc));
        dto.AdditionalFields.Should().ContainKey("payeeMCC");
    }

    // ─── Limite personalizado ──────────────────────────────────────────────────

    [Fact]
    public void ResolveCustomizedLimit_WhenSeveralLinesAreCustomized_ShouldReturnTheMostRestrictive()
    {
        var dto = DeserializeCreditData("""
        {
          "creditLimit": 10000,
          "availableCreditLimit": 8000,
          "disaggregatedCreditLimits": [
            { "customizedLimitAmount": 4000.00, "lineName": "CREDITO_A_VISTA" },
            { "customizedLimitAmount": 3000.00 },
            { "customizedLimitAmount": null }
          ]
        }
        """);

        PluggyCreditLimitResolver.ResolveCustomizedLimit(dto).Should().Be(3000.00m);
    }

    [Theory]
    [InlineData("""{ "customizedLimitAmount": "3500.50" }""", 3500.50)]
    [InlineData("""{ "customizedLimitAmount": 0 }""", null)]
    [InlineData("""{ "customizedLimitAmount": -10 }""", null)]
    [InlineData("""{ "customizedLimitAmount": { "amount": 3000 } }""", null)]
    [InlineData("""{ "customizedLimitAmount": "nao-numerico" }""", null)]
    [InlineData("""{ "lineName": "SEM_PERSONALIZACAO" }""", null)]
    public void ResolveCustomizedLimit_ShouldOnlyAcceptPositiveNumbersAndNeverThrow(string item, double? expected)
    {
        var dto = DeserializeCreditData($$"""{ "creditLimit": 10000, "disaggregatedCreditLimits": [ {{item}} ] }""");

        PluggyCreditLimitResolver.ResolveCustomizedLimit(dto).Should().Be((decimal?)expected);
    }

    [Fact]
    public void ResolveCustomizedLimit_WhenCreditDataHasNoDisaggregatedLimits_ShouldReturnNull()
    {
        PluggyCreditLimitResolver.ResolveCustomizedLimit(DeserializeCreditData("""{ "creditLimit": 10000 }"""))
            .Should().BeNull();
        PluggyCreditLimitResolver.ResolveCustomizedLimit(null).Should().BeNull();
    }

    private static PluggyCreditDataDto DeserializeCreditData(string json)
        => JsonSerializer.Deserialize<PluggyCreditDataDto>(
            json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;

    // ─── Parsing de datas ──────────────────────────────────────────────────────

    [Theory]
    [InlineData("2026-08-25")]
    [InlineData("2026-08-25T00:00:00.000Z")]
    public void ParseUtcDate_WhenValidDate_ShouldReturnUtc(string raw)
    {
        var parsed = PluggyAccount.ParseUtcDate(raw);

        parsed.Should().NotBeNull();
        parsed!.Value.Kind.Should().Be(DateTimeKind.Utc);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("nao-e-data")]
    public void ParseUtcDate_WhenAbsentOrMalformed_ShouldReturnNull(string? raw)
    {
        PluggyAccount.ParseUtcDate(raw).Should().BeNull();
    }

    // ─── Helpers ───────────────────────────────────────────────────────────────

    private static PluggyTransactionDto BuildTransaction(
        int? installmentNumber = null,
        int? totalInstallments = null)
        => new(
            Id: "tx-1",
            Description: "SUPERMERCADO EXEMPLO",
            Amount: 250.00m,
            Date: "2026-08-24T00:00:00.000Z",
            Type: "DEBIT",
            Category: "Supermercado",
            AccountId: "acc-card",
            CreditCardMetadata: installmentNumber is null && totalInstallments is null
                ? null
                : new PluggyCreditCardMetadataDto(
                    InstallmentNumber: installmentNumber,
                    TotalInstallments: totalInstallments));

    private static PluggyAccountDto BuildCreditAccount(string? balanceDueDate = "2026-08-25")
        => new(
            Id: "acc-card",
            Type: "CREDIT",
            Subtype: "CREDIT_CARD",
            Name: "Cartão",
            Balance: 2000.00m,
            CurrencyCode: "BRL",
            ItemId: "item-1",
            CreditData: new PluggyCreditDataDto(
                AvailableCreditLimit: 8000.00m,
                CreditLimit: 10000m,
                BalanceDueDate: balanceDueDate,
                BalanceCloseDate: null,
                MinimumPayment: 200.00m));

    private static PluggyAccountDto BuildCheckingAccount()
        => new(
            Id: "acc-checking",
            Type: "BANK",
            Subtype: "CHECKING_ACCOUNT",
            Name: "Conta Corrente",
            Balance: 1500.00m,
            CurrencyCode: "BRL",
            ItemId: "item-1",
            CreditData: null);
}
