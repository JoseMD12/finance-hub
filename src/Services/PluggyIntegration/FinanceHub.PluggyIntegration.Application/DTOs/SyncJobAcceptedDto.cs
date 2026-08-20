namespace FinanceHub.PluggyIntegration.Application.DTOs;

/// <summary>
/// DTO returned when an asynchronous synchronization job is accepted (HTTP 202 Accepted).
/// </summary>
public record SyncJobAcceptedDto(
    Guid JobId,
    string Status,
    string Message,
    DateTime StartedAtUtc
);
