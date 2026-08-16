namespace FinanceHub.MercadoPagoIntegration.Application.DTOs;

public record OpenFinanceSyncResultDto(
    Guid SyncId,
    string Status,
    int IngestedCount,
    DateTime LastSyncCursorUtc
);
