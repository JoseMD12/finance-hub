namespace FinanceHub.PluggyIntegration.Application.DTOs;

public record PluggyConnectorDto(
    int Id,
    string Name
);

public record PluggyItemDto(
    string Id,
    string Status,
    PluggyConnectorDto Connector
);
