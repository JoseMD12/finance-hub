namespace FinanceHub.PluggyIntegration.Application.DTOs;

public record PluggyConnectorDto(
    int Id,
    string Name
);

public record PluggyItemDto(
    string Id,
    string Status,
    PluggyConnectorDto Connector,
    decimal TotalBalance = 0m,
    int AccountsCount = 0,
    decimal TotalCredit = 0m,
    DateTime? LastUpdatedAt = null
);
