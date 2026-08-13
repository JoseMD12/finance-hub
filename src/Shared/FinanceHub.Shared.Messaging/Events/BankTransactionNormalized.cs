using System;

namespace FinanceHub.Shared.Messaging.Events;

public record BankTransactionNormalized(
    Guid TransactionId,
    string Source,
    string AccountId,
    decimal Amount,
    string Currency,
    string TransactionType,
    DateTime TransactionDate,
    string CleanDescription,
    string HashDeduplicacao,
    DateTime ProcessedAtUtc,
    Guid IngestionId,
    string? RawPayloadJson)
    : TransactionNormalized(TransactionId, Source, AccountId, Amount, Currency,
                            TransactionType, TransactionDate, CleanDescription,
                            HashDeduplicacao, ProcessedAtUtc);
