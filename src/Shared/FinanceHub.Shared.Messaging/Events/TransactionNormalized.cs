namespace FinanceHub.Shared.Messaging.Events;

/// <summary>
/// Event emitted by TransactionAggregator service after deduplicating and normalizing a transaction into the canonical ledger.
/// </summary>
public record TransactionNormalized(
    Guid TransactionId,
    Guid IngestionId,
    string Source,
    string AccountId,
    string Category,
    decimal Amount,
    DateTime TransactionDate,
    string CleanDescription,
    string HashDeduplicacao,
    DateTime ProcessedAtUtc
);
