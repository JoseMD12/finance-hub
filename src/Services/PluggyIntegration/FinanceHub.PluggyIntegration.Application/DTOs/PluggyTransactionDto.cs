using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace FinanceHub.PluggyIntegration.Application.DTOs;

/// <summary>
/// Metadados de cartão de crédito devolvidos pela Meu.Pluggy por transação. Todos os campos são
/// opcionais: nem todo connector informa parcelamento, e o plano gratuito varia por instituição.
/// </summary>
public record PluggyCreditCardMetadataDto(
    int? InstallmentNumber = null,
    int? TotalInstallments = null,
    string? BillId = null,
    /// <summary>
    /// Competência da fatura no formato <c>"YYYY-MM"</c>. É a Meu.Pluggy dizendo diretamente a
    /// qual fatura a compra pertence — dispensa derivar a data de fechamento. Confirmado em
    /// 27/08/2026: presente em ~67% das transações de cartão; quando ausente, a atribuição
    /// recai na regra de fechamento.
    /// </summary>
    string? BillForecastDate = null,
    /// <summary>
    /// Data real da compra, que difere da data de lançamento em parcelamentos: a parcela 2/2 é
    /// lançada meses depois da compra original.
    /// </summary>
    DateTime? PurchaseDate = null,
    string? CardNumber = null
)
{
    /// <summary>Campos ainda não mapeados, usados para descoberta do contrato real.</summary>
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? AdditionalFields { get; init; }
}

public record PluggyTransactionDto(
    string Id,
    string Description,
    decimal Amount,
    string Date,
    string? Type,
    string? Category,
    string? AccountId = null,
    PluggyCreditCardMetadataDto? CreditCardMetadata = null
);
