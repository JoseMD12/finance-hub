namespace FinanceHub.MercadoPagoIntegration.Application.Commands.SyncTransactions;

public record SyncMercadoPagoOpenFinanceCommand(
    string UserId,
    string ItemId,
    string? AccountId = null
);
