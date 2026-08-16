namespace FinanceHub.MercadoPagoIntegration.Application.DTOs;

public record SyncResultDto(
    Guid SyncId,
    string Status,
    int IngestedCount,
    DateTime LastSyncCursorUtc
);

public record MercadoPagoSyncRequestDto(
    string UserId,
    string? AccountId = null
);
