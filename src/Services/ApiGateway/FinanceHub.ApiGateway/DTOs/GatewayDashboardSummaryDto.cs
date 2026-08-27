namespace FinanceHub.ApiGateway.DTOs;

/// <summary>
/// Filtro do resumo do Dashboard repassado ao TransactionAggregator. <c>UserId</c> é sempre
/// extraído da claim do token pelo endpoint, nunca aceito da query string do cliente.
/// </summary>
public record GatewayDashboardFilterDto(
    string UserId,
    DateTime? StartDate = null,
    DateTime? EndDate = null,
    string? InstitutionId = null,
    bool IncludeIgnoredInTotals = false);

public record GatewayInstitutionBalanceDto(
    string InstitutionId,
    string AccountNumber,
    decimal BalanceBrl,
    string Currency,
    bool IsCreditCard,
    decimal? CreditLimit,
    decimal? AvailableCreditLimit,
    decimal? UsedCreditLimit,
    DateTime? InvoiceDueDateUtc,
    DateTime LastUpdatedAtUtc);

public record GatewayCategoryExpenseDto(
    Guid CategoryId,
    string CategoryName,
    string ColorToken,
    string IconKey,
    decimal AmountBrl,
    decimal Percentage);

public record GatewayMonthlyCashFlowPointDto(
    int Year,
    int Month,
    decimal IncomeBrl,
    decimal ExpenseBrl,
    decimal NetBrl);

/// <summary>
/// Resposta agregada do Dashboard. Substitui o antigo <see cref="DashboardResponseDto"/> na rota
/// <c>/api/v1/gateway/dashboard</c>; aquele contrato permanece intacto porque ainda serve
/// <c>/api/v1/gateway/balances/consolidated</c>.
/// </summary>
public record GatewayDashboardSummaryDto(
    string UserId,
    GatewayTransactionSummaryDto Summary,
    IReadOnlyList<GatewayInstitutionBalanceDto> InstitutionBalances,
    IReadOnlyList<GatewayCategoryExpenseDto> CategoryExpenses,
    IReadOnlyList<GatewayMonthlyCashFlowPointDto> MonthlyCashFlow,
    IReadOnlyList<GatewayTransactionDto> RecentTransactions,
    DateTime GeneratedAtUtc);
