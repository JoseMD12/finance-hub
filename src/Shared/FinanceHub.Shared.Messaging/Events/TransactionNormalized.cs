using System;

namespace FinanceHub.Shared.Messaging.Events;

public record TransactionNormalized(
    Guid TransactionId,
    string Source,
    string AccountId,
    decimal Amount,
    string Currency,
    string TransactionType,
    DateTime TransactionDate,
    string CleanDescription,
    string HashDeduplicacao,
    DateTime ProcessedAtUtc) : IFinanceHubEvent;
