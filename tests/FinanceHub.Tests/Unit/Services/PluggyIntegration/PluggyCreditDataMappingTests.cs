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
/// Cobre o mapeamento dos dados de crédito da Meu.Pluggy. Os casos refletem o payload real
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
        // Recorte fiel do payload devolvido pela API em 27/08/2026.
        const string json = """
        {
          "level": "GOLD",
          "brand": "MASTERCARD",
          "balanceCloseDate": null,
          "balanceDueDate": "2026-08-25",
          "availableCreditLimit": 9384.73,
          "minimumPayment": 250.67,
          "creditLimit": 12150,
          "status": "ACTIVE"
        }
        """;

        var dto = JsonSerializer.Deserialize<PluggyCreditDataDto>(
            json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;

        dto.CreditLimit.Should().Be(12150m);
        dto.AvailableCreditLimit.Should().Be(9384.73m);
        dto.BalanceDueDate.Should().Be("2026-08-25");
        dto.MinimumPayment.Should().Be(250.67m);

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
          "cardNumber": "2699",
          "payeeMCC": 5411,
          "billForecastDate": "2026-08",
          "installmentNumber": 2,
          "totalInstallments": 2,
          "purchaseDate": "2026-07-06T00:00:00.000Z",
          "billId": "7aad06fa-1b36-4af3-a404-22e91fe61883"
        }
        """;

        var dto = JsonSerializer.Deserialize<PluggyCreditCardMetadataDto>(
            json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;

        dto.BillForecastDate.Should().Be("2026-08");
        dto.BillId.Should().Be("7aad06fa-1b36-4af3-a404-22e91fe61883");
        dto.InstallmentNumber.Should().Be(2);
        dto.TotalInstallments.Should().Be(2);

        // Parcela lançada em agosto, mas comprada em julho: as duas datas são diferentes.
        dto.PurchaseDate.Should().Be(new DateTime(2026, 7, 6, 0, 0, 0, DateTimeKind.Utc));
        dto.AdditionalFields.Should().ContainKey("payeeMCC");
    }

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
            Description: "CARREFOUR PPA 106",
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
            Balance: 2765.27m,
            CurrencyCode: "BRL",
            ItemId: "item-1",
            CreditData: new PluggyCreditDataDto(
                AvailableCreditLimit: 9384.73m,
                CreditLimit: 12150m,
                BalanceDueDate: balanceDueDate,
                BalanceCloseDate: null,
                MinimumPayment: 250.67m));

    private static PluggyAccountDto BuildCheckingAccount()
        => new(
            Id: "acc-checking",
            Type: "BANK",
            Subtype: "CHECKING_ACCOUNT",
            Name: "Conta Corrente",
            Balance: 1650.59m,
            CurrencyCode: "BRL",
            ItemId: "item-1",
            CreditData: null);
}
