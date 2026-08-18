namespace FinanceHub.PluggyIntegration.Application.Commands.SyncAllPluggyAccounts;

public record SyncSinglePluggyItemCommand(
    string ItemId,
    string? UserId,
    string PluggyAccessToken
);
