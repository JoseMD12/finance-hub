using System;

namespace FinanceHub.TransactionAggregator.Domain.ValueObjects;

public record AccountIdentifier(string InstitutionId, string AccountId)
{
    public AccountIdentifier() : this(string.Empty, string.Empty) { }
}

public record TransactionAuditInfo(DateTime CreatedAtUtc, DateTime UpdatedAtUtc)
{
    public TransactionAuditInfo() : this(DateTime.UtcNow, DateTime.UtcNow) { }
}
