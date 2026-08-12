namespace FinanceHub.Shared.Messaging.Events;

/// <summary>
/// Event emitted by TransactionAggregator service after deduplicating and normalizing
/// a raw transaction into the canonical ledger. Consumed by downstream services
/// (reporting, budgeting, notifications).
/// </summary>
public record TransactionNormalized(
    Guid TransactionId,
    Guid IngestionId,
    string Source,
    string AccountId,
    string Category,
    decimal Amount,
    string Currency,
    string TransactionType,
    DateTime TransactionDate,
    string CleanDescription,
    string HashDeduplicacao,
    DateTime ProcessedAtUtc);
