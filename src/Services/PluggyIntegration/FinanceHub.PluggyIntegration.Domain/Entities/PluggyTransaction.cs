using System.Globalization;
using FinanceHub.PluggyIntegration.Domain.Constants;

namespace FinanceHub.PluggyIntegration.Domain.Entities;

public sealed class PluggyTransaction
{
    public string Id { get; private set; }
    public string Description { get; private set; }
    public decimal Amount { get; private set; }
    public string RawDate { get; private set; }
    public string? Category { get; private set; }
    public string AccountId { get; private set; }

    internal PluggyTransaction(
        string id,
        string description,
        decimal amount,
        string rawDate,
        string? category,
        string accountId)
    {
        Id = string.IsNullOrWhiteSpace(id) ? throw new ArgumentException("Id da transação é obrigatório.", nameof(id)) : id;
        Description = description ?? string.Empty;
        Amount = amount;
        RawDate = rawDate ?? string.Empty;
        Category = category;
        AccountId = accountId ?? string.Empty;
    }

    public DateTime ParseTransactionDate()
    {
        return DateTime.TryParse(RawDate, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var dt)
            ? DateTime.SpecifyKind(dt, DateTimeKind.Utc)
            : DateTime.UtcNow;
    }

    public string GetCanonicalCategory() => PluggyCategoryMapper.Map(Category);
}
