using System;
using System.Text.RegularExpressions;

namespace FinanceHub.TransactionAggregator.Domain.ValueObjects;

public record SanitizedDescription
{
    private static readonly Regex PrefixRegex = new(@"\b(PAG\*|DB\*|PIX\*|TED\*)\b", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex CityStateDateRegex = new(@"\s+\d{2}/\d{2}\s+.*$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public string OriginalText { get; }
    public string CleanText { get; }

    public SanitizedDescription(string originalText, string cleanText)
    {
        OriginalText = originalText ?? string.Empty;
        CleanText = cleanText ?? string.Empty;
    }

    public static SanitizedDescription Create(string originalDescription)
    {
        var raw = originalDescription ?? string.Empty;
        var clean = PrefixRegex.Replace(raw, string.Empty);
        clean = CityStateDateRegex.Replace(clean, string.Empty).Trim();

        return new SanitizedDescription(raw, clean);
    }
}
