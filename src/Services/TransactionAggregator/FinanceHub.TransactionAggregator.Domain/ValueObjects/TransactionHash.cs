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

    public static TransactionHash ComputeHash(
        string institutionId,
        string accountId,
        string bankTransactionId,
        decimal amount,
        System.DateTime dateUtc)
    {
        var rawKey = $"{institutionId}:{accountId}:{bankTransactionId}:{amount:F2}:{dateUtc:yyyy-MM-ddTHH:mm:ssZ}";
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var bytes = System.Text.Encoding.UTF8.GetBytes(rawKey);
        var hashBytes = sha256.ComputeHash(bytes);
        var hex = System.Convert.ToHexStringLower(hashBytes);
        return new TransactionHash(hex);
    }
}
