namespace FinanceHub.Shared.Messaging.Events;

/// <summary>
/// Event emitted when a credit card transaction or invoice item is ingested.
/// </summary>
public record InvoiceItemIngested(
    Guid IngestionId,
    string? UserId,
    string Source, // "Itau", "MercadoPago", "Inter", "MeuPluggy"
    string CreditCardAccountId,
    string? CardLastFourDigits,
    string? BankTransactionId,
    decimal Amount,
    DateTime TransactionDate,
    string Description,
    string? Category,
    int? CurrentInstallment,
    int? TotalInstallments,
    DateTime? InvoiceDueDate,
    string Currency,
    string? RawPayloadJson,
    DateTime OccurredAtUtc
) : IFinanceHubEvent, IIdempotentEvent
{
    public string MessageHash => IngestionId.ToString();
}
