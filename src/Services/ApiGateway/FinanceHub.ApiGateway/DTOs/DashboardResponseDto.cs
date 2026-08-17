namespace FinanceHub.ApiGateway.DTOs;

public record DashboardResponseDto(
    string UserId,
    decimal TotalBalanceBrl,
    IEnumerable<AccountBalanceSummaryDto> AccountBalances,
    DateTime GeneratedAtUtc);

public record AccountBalanceSummaryDto(
    string InstitutionId,
    string AccountNumber,
    decimal Amount,
    string Currency,
    DateTime LastUpdatedAtUtc);
