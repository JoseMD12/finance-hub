using System.Net.Http.Json;
using FinanceHub.ApiGateway.DTOs;
using FinanceHub.Shared.Messaging.Constants;
using Microsoft.Extensions.Logging;

namespace FinanceHub.ApiGateway.Clients;

public sealed class PluggyIntegrationServiceClient(
    HttpClient httpClient,
    ILogger<PluggyIntegrationServiceClient> logger) : IPluggyIntegrationServiceClient
{
    public async Task<IReadOnlyList<GatewayPluggyItemDto>> GetItemsAsync(string pluggyAccessToken, CancellationToken ct = default)
    {
        logger.LogInformation("Buscando itens conectados via downstream PluggyIntegration...");

        var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/pluggy/items");
        if (!string.IsNullOrWhiteSpace(pluggyAccessToken))
        {
            request.Headers.Add(FinanceHubHeaderNames.PluggyAccessToken, pluggyAccessToken);
        }

        var response = await httpClient.SendAsync(request, ct);
        await EnsureSuccessOrThrowAsync(response, ct);

        var items = await response.Content.ReadFromJsonAsync<IReadOnlyList<GatewayPluggyItemDto>>(cancellationToken: ct);
        return items ?? [];
    }

    public async Task<IReadOnlyList<GatewayPluggyAccountDto>> GetAccountsAsync(string pluggyAccessToken, CancellationToken ct = default)
    {
        logger.LogInformation("Buscando contas conectadas via downstream PluggyIntegration...");

        var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/pluggy/accounts");
        if (!string.IsNullOrWhiteSpace(pluggyAccessToken))
        {
            request.Headers.Add(FinanceHubHeaderNames.PluggyAccessToken, pluggyAccessToken);
        }

        var response = await httpClient.SendAsync(request, ct);
        await EnsureSuccessOrThrowAsync(response, ct);

        var accounts = await response.Content.ReadFromJsonAsync<IReadOnlyList<GatewayPluggyAccountDto>>(cancellationToken: ct);
        return accounts ?? [];
    }

    public async Task<GatewaySyncJobAcceptedDto?> TriggerSyncAsync(string? userId, string pluggyAccessToken, CancellationToken ct = default)
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
        await EnsureSuccessOrThrowAsync(response, ct);

        return await response.Content.ReadFromJsonAsync<GatewaySyncJobAcceptedDto>(cancellationToken: ct);
    }

    public async Task<GatewaySyncJobStatusDto?> GetSyncJobStatusAsync(Guid jobId, CancellationToken ct = default)
    {
        logger.LogInformation("Consultando status do job de sincronização {JobId} via downstream PluggyIntegration...", jobId);

        var response = await httpClient.GetAsync($"/api/v1/pluggy/sync/jobs/{jobId}", ct);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }

        await EnsureSuccessOrThrowAsync(response, ct);
        return await response.Content.ReadFromJsonAsync<GatewaySyncJobStatusDto>(cancellationToken: ct);
    }

    public async Task<GatewayPluggySyncSummaryDto?> ResyncItemAsync(string itemId, string? userId, string pluggyAccessToken, CancellationToken ct = default)
    {
        logger.LogInformation("Solicitando ressincronização da instituição {ItemId} via downstream PluggyIntegration...", itemId);

        var endpoint = $"/api/v1/pluggy/items/{Uri.EscapeDataString(itemId)}/sync";
        if (!string.IsNullOrWhiteSpace(userId))
        {
            endpoint += $"?userId={Uri.EscapeDataString(userId)}";
        }

        var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
        if (!string.IsNullOrWhiteSpace(pluggyAccessToken))
        {
            request.Headers.Add(FinanceHubHeaderNames.PluggyAccessToken, pluggyAccessToken);
        }

        var response = await httpClient.SendAsync(request, ct);
        await EnsureSuccessOrThrowAsync(response, ct);
        return await response.Content.ReadFromJsonAsync<GatewayPluggySyncSummaryDto>(cancellationToken: ct);
    }

    private static async Task EnsureSuccessOrThrowAsync(HttpResponseMessage response, CancellationToken ct)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var detail = await response.Content.ReadAsStringAsync(ct);
        throw new HttpRequestException(
            $"PluggyIntegration returned {(int)response.StatusCode} ({response.ReasonPhrase}). Detail: {detail}",
            inner: null,
            statusCode: response.StatusCode);
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
