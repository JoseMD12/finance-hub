using System;

namespace FinanceHub.TransactionAggregator.Application.DTOs;

public record TransactionFilterDto(
    string UserId,
    int Page = 1,
    int PageSize = 20,
    DateTime? StartDate = null,
    DateTime? EndDate = null,
    string? InstitutionId = null,
    Guid? CategoryId = null,
    string? Type = null,
    string? Search = null);
