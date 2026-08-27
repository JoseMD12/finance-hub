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
    public string? RawBalanceCloseDate { get; private set; }
    public decimal? CreditLimit { get; private set; }
    public decimal? AvailableCreditLimit { get; private set; }

    internal PluggyAccount(
        string id,
        string type,
        string? subtype,
        string name,
        decimal balance,
        string? currencyCode,
        string? rawBalanceDueDate,
        string? rawBalanceCloseDate = null,
        decimal? creditLimit = null,
        decimal? availableCreditLimit = null)
    {
        Id = string.IsNullOrWhiteSpace(id) ? throw new ArgumentException("Id da conta é obrigatório.", nameof(id)) : id;
        TypeInfo = new AccountType(type, subtype);
        Name = name ?? string.Empty;
        Balance = balance;
        CurrencyCode = string.IsNullOrWhiteSpace(currencyCode) ? PluggyConstants.DefaultCurrency : currencyCode;
        RawBalanceDueDate = rawBalanceDueDate;
        RawBalanceCloseDate = rawBalanceCloseDate;
        CreditLimit = creditLimit;
        AvailableCreditLimit = availableCreditLimit;
    }

    public DateTime? ParseDueDate() => ParseUtcDate(RawBalanceDueDate);

    /// <summary>
    /// Fechamento da fatura. Nem todo connector do plano gratuito informa este campo; quando
    /// ausente, o consumidor recorre ao vencimento menos um deslocamento configurável.
    /// </summary>
    public DateTime? ParseCloseDate() => ParseUtcDate(RawBalanceCloseDate);

    /// <summary>
    /// Converte uma data crua da Meu.Pluggy (ex.: <c>"2026-08-25"</c>) para UTC, devolvendo
    /// <c>null</c> quando ausente ou em formato inesperado.
    /// </summary>
    public static DateTime? ParseUtcDate(string? rawDate)
    {
        if (!string.IsNullOrWhiteSpace(rawDate) &&
            DateTime.TryParse(rawDate, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var parsed))
        {
            return DateTime.SpecifyKind(parsed, DateTimeKind.Utc);
        }

        return null;
    }
}
