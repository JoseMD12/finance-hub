using FinanceHub.ApiGateway.DTOs;

namespace FinanceHub.ApiGateway.Clients;

public interface IPluggyIntegrationServiceClient
{
    Task<GatewayPluggySyncSummaryDto?> TriggerSyncAsync(string? userId, string pluggyAccessToken, CancellationToken ct = default);
    Task<bool> HealthCheckAsync(CancellationToken ct = default);
}
