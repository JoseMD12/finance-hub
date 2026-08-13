using System;

namespace FinanceHub.Shared.Messaging.Events;

public record TransactionCategorized(
    Guid TransactionId,
    Guid CategoryId,
    string CategoryName,
    string CategorizationSource,
    DateTime CategorizedAtUtc) : IFinanceHubEvent;
