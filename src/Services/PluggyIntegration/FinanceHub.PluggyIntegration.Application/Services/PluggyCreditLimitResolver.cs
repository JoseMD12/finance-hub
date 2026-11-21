using System.Globalization;
using System.Text.Json;
using FinanceHub.PluggyIntegration.Application.DTOs;

namespace FinanceHub.PluggyIntegration.Application.Services;

/// <summary>
/// Extrai o limite que o próprio titular definiu no app do banco, quase sempre menor que o
/// <c>creditLimit</c> concedido. Medir o consumo contra o limite do banco subestima o uso real.
/// </summary>
public static class PluggyCreditLimitResolver
{
    /// <summary>
    /// Com mais de uma linha personalizada, vale a mais restritiva: é ela que bloqueia a compra.
    /// Zero e negativos são tratados como ausência de personalização.
    /// </summary>
    public static decimal? ResolveCustomizedLimit(PluggyCreditDataDto? creditData)
    {
        var limits = creditData?.DisaggregatedCreditLimits;
        if (limits is null)
        {
            return null;
        }

        decimal? mostRestrictive = null;
        foreach (var limit in limits)
        {
            var amount = ReadPositiveAmount(limit?.CustomizedLimitAmount);
            if (amount is not null && (mostRestrictive is null || amount < mostRestrictive))
            {
                mostRestrictive = amount;
            }
        }

        return mostRestrictive;
    }

    private static decimal? ReadPositiveAmount(JsonElement? element)
    {
        if (element is null)
        {
            return null;
        }

        var value = element.Value;
        var amount = 0m;
        var parsed = value.ValueKind switch
        {
            JsonValueKind.Number => value.TryGetDecimal(out amount),
            JsonValueKind.String => decimal.TryParse(
                value.GetString(), NumberStyles.Number, CultureInfo.InvariantCulture, out amount),
            _ => false
        };

        return parsed && amount > 0m ? amount : null;
    }
}
