using System;

namespace FinanceHub.TransactionAggregator.Api.Endpoints;

public sealed record GetTransactionsParameters(
    string UserId,
    int? Page,
    int? PageSize,
    DateTime? StartDate,
    DateTime? EndDate,
    string? InstitutionId,
    Guid? CategoryId,
    string? Type,
    string? Search);
