namespace FinanceHub.PluggyIntegration.Application.DTOs;

public record SyncJobStatusDto(
    Guid JobId,
    string Status,
    string Message,
    DateTime StartedAtUtc,
    DateTime? CompletedAtUtc,
    SyncPluggySummaryDto? Result,
    string? ErrorMessage = null
);
