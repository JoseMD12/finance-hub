using System;

namespace FinanceHub.TransactionAggregator.Domain.ValueObjects;

public record AccountIdentifier(string InstitutionId, string AccountId);

public record TransactionAuditInfo(DateTime CreatedAtUtc, DateTime UpdatedAtUtc);
