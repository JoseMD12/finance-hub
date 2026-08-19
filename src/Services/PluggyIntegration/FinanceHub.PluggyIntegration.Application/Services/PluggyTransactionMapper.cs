using FinanceHub.PluggyIntegration.Application.DTOs;
using FinanceHub.PluggyIntegration.Domain.Aggregates;
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
        var session = PluggySyncSessionAggregate.Create(userId, sourceName);

        var domainAccount = session.RegisterAccount(
            account.Id,
            account.Type,
            account.Subtype,
            account.Name,
            account.Balance,
            account.CurrencyCode,
            account.CreditData?.BalanceDueDate
        );

        var domainTx = session.RecordTransaction(
            tx.Id,
            tx.Description,
            tx.Amount,
            tx.Date,
            tx.Category,
            tx.AccountId ?? string.Empty
        );

        if (domainAccount.TypeInfo.IsCreditCard)
        {
            cardEvents.Add(new InvoiceItemIngested(
                IngestionId: session.SessionId,
                UserId: session.UserId,
                Source: session.SourceName,
                CreditCardAccountId: domainAccount.Id,
                CardLastFourDigits: null,
                BankTransactionId: domainTx.Id,
                Amount: domainTx.Amount,
                TransactionDate: domainTx.ParseTransactionDate(),
                Description: domainTx.Description,
                Category: domainTx.GetCanonicalCategory(),
                CurrentInstallment: null,
                TotalInstallments: null,
                InvoiceDueDate: domainAccount.ParseDueDate(),
                Currency: domainAccount.CurrencyCode,
                RawPayloadJson: null,
                OccurredAtUtc: DateTime.UtcNow
            ));
        }
        else
        {
            checkingEvents.Add(new TransactionIngested(
                IngestionId: session.SessionId,
                UserId: session.UserId,
                Source: session.SourceName,
                AccountId: domainAccount.Id,
                BankTransactionId: domainTx.Id,
                Amount: domainTx.Amount,
                TransactionDate: domainTx.ParseTransactionDate(),
                Description: domainTx.Description,
                Currency: domainAccount.CurrencyCode,
                RawPayloadJson: null,
                OccurredAtUtc: DateTime.UtcNow
            ));
        }
    }
}
