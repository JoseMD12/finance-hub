using System.Collections.Generic;
using FinanceHub.ApiGateway.DTOs;

namespace FinanceHub.ApiGateway.Clients;

public interface IPluggyIntegrationServiceClient
{
    Task<IReadOnlyList<GatewayPluggyItemDto>> GetItemsAsync(string pluggyAccessToken, CancellationToken ct = default);
    Task<IReadOnlyList<GatewayPluggyAccountDto>> GetAccountsAsync(string pluggyAccessToken, CancellationToken ct = default);
    Task<GatewayPluggySyncSummaryDto?> ResyncItemAsync(string itemId, string? userId, string pluggyAccessToken, CancellationToken ct = default);
    Task<GatewaySyncJobAcceptedDto?> TriggerSyncAsync(string? userId, string pluggyAccessToken, CancellationToken ct = default);
    Task<GatewaySyncJobStatusDto?> GetSyncJobStatusAsync(Guid jobId, CancellationToken ct = default);
    Task<bool> HealthCheckAsync(CancellationToken ct = default);
}
