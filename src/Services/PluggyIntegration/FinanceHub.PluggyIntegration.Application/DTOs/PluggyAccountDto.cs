using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace FinanceHub.PluggyIntegration.Application.DTOs;

public record PluggyCreditDataDto(
    decimal? AvailableCreditLimit,
    decimal? CreditLimit,
    string? BalanceDueDate,
    /// <summary>
    /// Data de fechamento da fatura. Confirmado em 27/08/2026: a Meu.Pluggy expõe o campo, mas
    /// devolveu <c>null</c> nos três cartões testados (Itaú, Inter e Mercado Pago). O fechamento
    /// portanto precisa vir da cascata de fallback, não desta propriedade.
    /// </summary>
    string? BalanceCloseDate = null,
    decimal? MinimumPayment = null
)
{
    /// <summary>
    /// Captura todo campo de <c>creditData</c> que ainda não mapeamos. Serve de instrumento de
    /// descoberta: permite registrar em log exatamente o que o plano gratuito da Meu.Pluggy
    /// devolve por connector, sem depender de suposição sobre o contrato da API.
    /// </summary>
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? AdditionalFields { get; init; }

    public IReadOnlyList<PluggyDisaggregatedCreditLimitDto>? DisaggregatedCreditLimits { get; init; }
}

/// <summary>
/// Item de <c>creditData.disaggregatedCreditLimits</c>. A Pluggy declara a lista, mas não documenta
/// os campos do item; <c>customizedLimitAmount</c> foi observado na API real em 27/08/2026. Por
/// isso o campo é lido como JSON bruto: um tipo inesperado vira "sem personalização" em vez de
/// derrubar a desserialização da conta e, com ela, o sync inteiro.
/// </summary>
public record PluggyDisaggregatedCreditLimitDto
{
    public JsonElement? CustomizedLimitAmount { get; init; }

    [JsonExtensionData]
    public IDictionary<string, JsonElement>? AdditionalFields { get; init; }
}

public record PluggyAccountDto(
    string Id,
    string Type,
    string Subtype,
    string Name,
    decimal Balance,
    string CurrencyCode,
    string ItemId,
    PluggyCreditDataDto? CreditData
);
