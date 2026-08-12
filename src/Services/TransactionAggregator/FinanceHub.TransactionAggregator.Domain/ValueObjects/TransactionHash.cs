using System.Text.RegularExpressions;
using FinanceHub.TransactionAggregator.Domain.Exceptions;

namespace FinanceHub.TransactionAggregator.Domain.ValueObjects;

public record TransactionHash
{
    private static readonly Regex Hex64Regex = new("^[a-fA-F0-9]{64}$", RegexOptions.Compiled);

    public string Value { get; }

    public TransactionHash(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || !Hex64Regex.IsMatch(value))
        {
            throw new InvalidTransactionHashDomainException();
        }

        Value = value.ToLowerInvariant();
    }
}
