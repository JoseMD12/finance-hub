namespace FinanceHub.ApiGateway.Clients;

public record GatewayConnectTokenResultDto(string AccessToken, DateTime ExpiresAtUtc);
public record GatewaySyncResultDto(Guid SyncId, string Status, int IngestedCount, DateTime LastSyncCursorUtc);

public interface IMercadoPagoServiceClient
{
    Task<GatewayConnectTokenResultDto> CreateConnectTokenAsync(string userId, string? itemId = null, CancellationToken ct = default);
    Task<GatewaySyncResultDto> TriggerSyncAsync(string userId, string itemId, string? accountId = null, CancellationToken ct = default);
}
