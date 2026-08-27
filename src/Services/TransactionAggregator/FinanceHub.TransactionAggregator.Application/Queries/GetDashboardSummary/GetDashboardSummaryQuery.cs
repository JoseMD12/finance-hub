using System;

namespace FinanceHub.TransactionAggregator.Application.Queries.GetDashboardSummary;

/// <summary>
/// Consulta agregada do Dashboard para um período. <c>UserId</c> vem sempre da claim do token
/// no Gateway, nunca da query string.
/// </summary>
public record GetDashboardSummaryQuery(
    string UserId,
    DateTime? StartDate = null,
    DateTime? EndDate = null,
    string? InstitutionId = null,
    bool IncludeIgnoredInTotals = false);
