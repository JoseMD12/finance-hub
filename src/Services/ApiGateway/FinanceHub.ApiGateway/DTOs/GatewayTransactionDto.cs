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
