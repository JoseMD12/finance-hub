using System.Globalization;
using FinanceHub.PluggyIntegration.Application.DTOs;
using FinanceHub.PluggyIntegration.Domain.Constants;
using FinanceHub.Shared.Messaging.Events;

namespace FinanceHub.PluggyIntegration.Application.Services;

public sealed class PluggyTransactionMapper : IPluggyTransactionMapper
{
    public void MapTransactionToEvents(
        PluggyTransactionDto tx,
        PluggyAccountDto account,
        string sourceName,
        string userId,
        List<TransactionIngested> checkingEvents,
        List<InvoiceItemIngested> cardEvents)
    {
        var txDate = ParseDate(tx.Date);

        if (IsCreditCardAccount(account))
        {
            DateTime? dueDate = ParseDueDate(account.CreditData?.BalanceDueDate);
            var canonicalCategory = PluggyCategoryMapper.Map(tx.Category);

            cardEvents.Add(new InvoiceItemIngested(
                IngestionId: Guid.NewGuid(),
                UserId: userId,
                Source: sourceName,
                CreditCardAccountId: account.Id,
                CardLastFourDigits: null,
                BankTransactionId: tx.Id,
                Amount: tx.Amount,
                TransactionDate: txDate,
                Description: tx.Description,
                Category: canonicalCategory,
                CurrentInstallment: null,
                TotalInstallments: null,
                InvoiceDueDate: dueDate,
                Currency: account.CurrencyCode ?? PluggyConstants.DefaultCurrency,
                RawPayloadJson: null,
                OccurredAtUtc: DateTime.UtcNow
            ));
        }
        else
        {
            checkingEvents.Add(new TransactionIngested(
                IngestionId: Guid.NewGuid(),
                UserId: userId,
                Source: sourceName,
                AccountId: account.Id,
                BankTransactionId: tx.Id,
                Amount: tx.Amount,
                TransactionDate: txDate,
                Description: tx.Description,
                Currency: account.CurrencyCode ?? PluggyConstants.DefaultCurrency,
                RawPayloadJson: null,
                OccurredAtUtc: DateTime.UtcNow
            ));
        }
    }

    private static bool IsCreditCardAccount(PluggyAccountDto account) =>
        account.Type == PluggyConstants.AccountTypes.Credit ||
        account.Subtype == PluggyConstants.AccountSubtypes.CreditCard;

    private static DateTime? ParseDueDate(string? rawDueDate)
    {
        if (!string.IsNullOrWhiteSpace(rawDueDate) &&
            DateTime.TryParse(rawDueDate, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var parsedDueDate))
        {
            return DateTime.SpecifyKind(parsedDueDate, DateTimeKind.Utc);
        }

        return null;
    }

    private static DateTime ParseDate(string? rawDate)
    {
        return DateTime.TryParse(rawDate, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var dt)
            ? DateTime.SpecifyKind(dt, DateTimeKind.Utc)
            : DateTime.UtcNow;
    }
}
