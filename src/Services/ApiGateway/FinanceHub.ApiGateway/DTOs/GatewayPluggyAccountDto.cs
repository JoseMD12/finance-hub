namespace FinanceHub.ApiGateway.DTOs;

public record GatewayPluggyCreditDataDto(
    decimal? AvailableCreditLimit,
    decimal? CreditLimit,
    string? BalanceDueDate
);

public record GatewayPluggyAccountDto(
    string ItemId,
    string InstitutionName,
    string Name,
    string Type,
    string Subtype,
    decimal Balance,
    GatewayPluggyCreditDataDto? CreditData
);
