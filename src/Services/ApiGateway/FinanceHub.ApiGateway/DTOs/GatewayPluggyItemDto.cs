namespace FinanceHub.ApiGateway.DTOs;

public record GatewayPluggyConnectorDto(
    int Id,
    string Name
);

public record GatewayPluggyItemDto(
    string Id,
    string Status,
    GatewayPluggyConnectorDto Connector,
    decimal TotalBalance = 0m,
    int AccountsCount = 0,
    decimal TotalCredit = 0m,
    DateTime? LastUpdatedAt = null
);
