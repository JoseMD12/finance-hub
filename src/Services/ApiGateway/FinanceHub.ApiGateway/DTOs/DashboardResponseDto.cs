namespace FinanceHub.ApiGateway.DTOs;

public record DashboardResponseDto(
    string UserId,
    decimal TotalBalanceBrl,
    IEnumerable<AccountBalanceSummaryDto> AccountBalances,
    IEnumerable<ActiveConsentSummaryDto> ActiveConsents,
    DateTime GeneratedAtUtc);

public record AccountBalanceSummaryDto(
    string InstitutionId,
    string AccountNumber,
    decimal Amount,
    string Currency,
    DateTime LastUpdatedAtUtc);

public record ActiveConsentSummaryDto(
    Guid ConsentId,
    string InstitutionId,
    string Status,
    DateTime? ExpiresAtUtc);
