using System;
using System.Collections.Generic;

namespace FinanceHub.TransactionAggregator.Application.DTOs;

public record TransactionSummaryDto(
    decimal TotalIncome,
    decimal TotalExpense,
    decimal NetBalance,
    int TotalCount);

public record PagedTransactionsResponseDto(
    IEnumerable<TransactionDto> Items,
    TransactionSummaryDto Summary,
    int Page,
    int PageSize,
    int TotalItems,
    int TotalPages);
