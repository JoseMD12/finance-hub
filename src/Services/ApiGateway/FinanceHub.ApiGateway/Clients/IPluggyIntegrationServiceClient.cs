using FinanceHub.ApiGateway.DTOs;

namespace FinanceHub.ApiGateway.Clients;

public interface IPluggyIntegrationServiceClient
{
    Task<GatewayPluggySyncSummaryDto?> TriggerSyncAsync(string? userId = null, CancellationToken ct = default);
    Task<bool> HealthCheckAsync(CancellationToken ct = default);
}
