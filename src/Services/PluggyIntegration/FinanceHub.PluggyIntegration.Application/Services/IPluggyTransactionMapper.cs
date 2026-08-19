using FinanceHub.PluggyIntegration.Application.DTOs;
using FinanceHub.Shared.Messaging.Events;

namespace FinanceHub.PluggyIntegration.Application.Services;

public interface IPluggyTransactionMapper
{
    void MapTransactionToEvents(
        PluggyTransactionDto tx,
        PluggyAccountDto account,
        string sourceName,
        string userId,
        List<TransactionIngested> checkingEvents,
        List<InvoiceItemIngested> cardEvents);
}
