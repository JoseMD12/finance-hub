using System.Globalization;
using FinanceHub.PluggyIntegration.Domain.Constants;
using FinanceHub.PluggyIntegration.Domain.ValueObjects;

namespace FinanceHub.PluggyIntegration.Domain.Entities;

public sealed class PluggyAccount
{
    public string Id { get; private set; }
    public AccountType TypeInfo { get; private set; }
    public string Name { get; private set; }
    public decimal Balance { get; private set; }
    public string CurrencyCode { get; private set; }
    public string? RawBalanceDueDate { get; private set; }

    internal PluggyAccount(
        string id,
        string type,
        string? subtype,
        string name,
        decimal balance,
        string? currencyCode,
        string? rawBalanceDueDate)
    {
        Id = string.IsNullOrWhiteSpace(id) ? throw new ArgumentException("Id da conta é obrigatório.", nameof(id)) : id;
        TypeInfo = new AccountType(type, subtype);
        Name = name ?? string.Empty;
        Balance = balance;
        CurrencyCode = string.IsNullOrWhiteSpace(currencyCode) ? PluggyConstants.DefaultCurrency : currencyCode;
        RawBalanceDueDate = rawBalanceDueDate;
    }

    public DateTime? ParseDueDate()
    {
        if (!string.IsNullOrWhiteSpace(RawBalanceDueDate) &&
            DateTime.TryParse(RawBalanceDueDate, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var parsedDueDate))
        {
            return DateTime.SpecifyKind(parsedDueDate, DateTimeKind.Utc);
        }

        return null;
    }
}
