namespace FinanceHub.Shared.Messaging.Events;

/// <summary>
/// Event emitted when a chunked batch of raw transactions is ingested from an Open Finance connector or File Importer.
/// Allows bulk deduplication and atomic persistence at the aggregator.
/// </summary>
public record TransactionsBatchIngested(
    Guid BatchId,
    string? UserId,
    int ChunkIndex,
    int TotalChunks,
    IReadOnlyList<TransactionIngested> CheckingTransactions,
    IReadOnlyList<InvoiceItemIngested> CardTransactions,
    DateTime OccurredAtUtc
) : IFinanceHubEvent, IIdempotentEvent
{
    public string MessageHash => BatchId.ToString();
}
