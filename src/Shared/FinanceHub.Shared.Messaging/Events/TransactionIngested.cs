namespace FinanceHub.Shared.Messaging.Events;

/// <summary>
/// Event emitted when a raw transaction is ingested from an Open Finance connector or File Importer.
/// </summary>
public record TransactionIngested(
    Guid IngestionId,
    string? UserId,
    string Source, // "Itau", "MercadoPago", "Inter", "MeuPluggy"
    string AccountId,
    string? BankTransactionId,
    decimal Amount,
    DateTime TransactionDate,
    string Description,
    string Currency,
    string? RawPayloadJson,
    DateTime OccurredAtUtc
) : IFinanceHubEvent, IIdempotentEvent
{
    public string MessageHash => IngestionId.ToString();
}
