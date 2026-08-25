namespace FinanceHub.ApiGateway.DTOs;

public record GatewaySyncJobAcceptedDto(
    Guid JobId,
    string Status,
    string Message,
    DateTime StartedAtUtc
);
