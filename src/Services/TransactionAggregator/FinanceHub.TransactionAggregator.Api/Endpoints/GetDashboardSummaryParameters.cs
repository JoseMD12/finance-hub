using System;

namespace FinanceHub.TransactionAggregator.Api.Endpoints;

/// <summary>
/// Parâmetros de query do resumo do Dashboard. <c>UserId</c> chega do Gateway, que o extrai da
/// claim do token — nunca é informado diretamente pelo cliente final.
/// </summary>
public sealed record GetDashboardSummaryParameters(
    string UserId,
    DateTime? StartDate,
    DateTime? EndDate,
    string? InstitutionId,
    bool? IncludeIgnoredInTotals);
