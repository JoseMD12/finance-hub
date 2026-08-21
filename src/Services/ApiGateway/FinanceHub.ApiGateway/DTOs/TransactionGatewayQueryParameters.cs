using System;

namespace FinanceHub.ApiGateway.DTOs;

public sealed record TransactionGatewayQueryParameters(
    int? Page,
    int? PageSize,
    DateTime? StartDate,
    DateTime? EndDate,
    string? InstitutionId,
    Guid? CategoryId,
    string? Type,
    string? Search);
