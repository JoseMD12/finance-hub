using System.Net.Http.Json;
using FinanceHub.ApiGateway.DTOs;
using Microsoft.Extensions.Logging;

namespace FinanceHub.ApiGateway.Clients;

public class PluggyIntegrationServiceClient : IPluggyIntegrationServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<PluggyIntegrationServiceClient> _logger;

    public PluggyIntegrationServiceClient(HttpClient httpClient, ILogger<PluggyIntegrationServiceClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<GatewayPluggySyncSummaryDto?> TriggerSyncAsync(string? userId = null, CancellationToken ct = default)
    {
        _logger.LogInformation("Disparando sincronização via downstream PluggyIntegration para UserId: {UserId}...", userId);

        var endpoint = !string.IsNullOrWhiteSpace(userId)
            ? $"/api/v1/pluggy/sync?userId={Uri.EscapeDataString(userId)}"
            : "/api/v1/pluggy/sync";

        var response = await _httpClient.PostAsync(endpoint, null, ct);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<GatewayPluggySyncSummaryDto>(cancellationToken: ct);
    }

    public async Task<bool> HealthCheckAsync(CancellationToken ct = default)
    {
        try
        {
            var response = await _httpClient.GetAsync("/health", ct);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Health check falhou para PluggyIntegrationServiceClient.");
            return false;
        }
    }
}
