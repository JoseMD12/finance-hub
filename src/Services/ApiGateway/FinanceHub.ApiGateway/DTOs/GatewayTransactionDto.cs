namespace FinanceHub.ApiGateway.DTOs;

public record GatewayTransactionDto(
    Guid Id,
    string UserId,
    string InstitutionId,
    string AccountNumber,
    decimal Amount,
    string Currency,
    string Type,
    string Description,
    Guid CategoryId,
    string CategorizationSource,
    bool IsManuallyCategorized,
    DateTime TransactionDateUtc,
    string Channel,
    string MerchantName);

public record GatewayConsolidatedBalanceDto(
    string UserId,
    decimal TotalBalanceBrl,
    IEnumerable<GatewayAccountBalanceDto> AccountBalances);

public record GatewayAccountBalanceDto(
    string InstitutionId,
    string AccountNumber,
    decimal Amount,
    string Currency,
    DateTime LastUpdatedAtUtc);

public record GatewayTransactionSummaryDto(
    decimal TotalIncome,
    decimal TotalExpense,
    decimal NetBalance,
    int TotalCount);

public record PagedGatewayTransactionsDto(
    IEnumerable<GatewayTransactionDto> Items,
    GatewayTransactionSummaryDto Summary,
    int Page,
    int PageSize,
    int TotalItems,
    int TotalPages);

public record GatewayTransactionFilterDto(
    string UserId,
    int Page = 1,
    int PageSize = 20,
    DateTime? StartDate = null,
    DateTime? EndDate = null,
    string? InstitutionId = null,
    Guid? CategoryId = null,
    string? Type = null,
    string? Search = null);
