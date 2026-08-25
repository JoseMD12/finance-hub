namespace FinanceHub.ApiGateway.DTOs;

public record GatewaySyncJobStatusDto(
    Guid JobId,
    string Status,
    string Message,
    DateTime StartedAtUtc,
    DateTime? CompletedAtUtc,
    GatewayPluggySyncSummaryDto? Result,
    string? ErrorMessage = null
);
