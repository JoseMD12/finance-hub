using System.Net.Http.Json;
using FinanceHub.ApiGateway.DTOs;
using FinanceHub.Shared.Messaging.Constants;
using Microsoft.Extensions.Logging;

namespace FinanceHub.ApiGateway.Clients;

public sealed class PluggyIntegrationServiceClient(
    HttpClient httpClient,
    ILogger<PluggyIntegrationServiceClient> logger) : IPluggyIntegrationServiceClient
{
    public async Task<GatewayPluggySyncSummaryDto?> TriggerSyncAsync(string? userId, string pluggyAccessToken, CancellationToken ct = default)
    {
        logger.LogInformation("Disparando sincronização via downstream PluggyIntegration para UserId: {UserId}...", userId);

        var endpoint = !string.IsNullOrWhiteSpace(userId)
            ? $"/api/v1/pluggy/sync?userId={Uri.EscapeDataString(userId)}"
            : "/api/v1/pluggy/sync";

        var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
        if (!string.IsNullOrWhiteSpace(pluggyAccessToken))
        {
            request.Headers.Add(FinanceHubHeaderNames.PluggyAccessToken, pluggyAccessToken);
        }

        var response = await httpClient.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<GatewayPluggySyncSummaryDto>(cancellationToken: ct);
    }

    public async Task<bool> HealthCheckAsync(CancellationToken ct = default)
    {
        try
        {
            var response = await httpClient.GetAsync("/health", ct);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Health check falhou para PluggyIntegrationServiceClient.");
            return false;
        }
    }
}
