using System;
using System.Collections.Generic;

namespace FinanceHub.TransactionAggregator.Domain.Constants;

public static class BankAliases
{
    private static readonly Dictionary<string, string[]> Aliases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["itau"] = ["itau", "itaú", "itauunibanco"],
        ["inter"] = ["inter", "bancointer", "banco inter"],
        ["mercadopago"] = ["mercadopago", "mercado pago", "mp", "mercado"],
        ["nubank"] = ["nubank", "nu"]
    };

    public static IReadOnlyList<string> GetKeywordsFor(string institutionId)
    {
        if (string.IsNullOrWhiteSpace(institutionId))
        {
            return Array.Empty<string>();
        }

        var key = institutionId.Trim();
        if (Aliases.TryGetValue(key, out var keywords))
        {
            return keywords;
        }

        return [key.ToLowerInvariant()];
    }
}
