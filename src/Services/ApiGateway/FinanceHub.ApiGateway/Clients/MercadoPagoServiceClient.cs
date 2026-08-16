using System.Net.Http.Json;
using FinanceHub.ApiGateway.Exceptions;

namespace FinanceHub.ApiGateway.Clients;

public class MercadoPagoServiceClient : IMercadoPagoServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<MercadoPagoServiceClient> _logger;

    public MercadoPagoServiceClient(HttpClient httpClient, ILogger<MercadoPagoServiceClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<GatewayConnectTokenResultDto> CreateConnectTokenAsync(string userId, string? itemId = null, CancellationToken ct = default)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/v1/mercadopago/connect-token", new { UserId = userId, ItemId = itemId }, ct);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<GatewayConnectTokenResultDto>(cancellationToken: ct);
                return result ?? new GatewayConnectTokenResultDto(string.Empty, DateTime.UtcNow.AddMinutes(30));
            }

            var errorContent = await response.Content.ReadAsStringAsync(ct);
            _logger.LogWarning("Falha ao criar ConnectToken Open Finance: {Status} - {Error}", response.StatusCode, errorContent);
            throw new GatewayDownstreamException(GatewayConstants.Downstream.MercadoPagoIntegrationServiceName, $"Status {(int)response.StatusCode}: {errorContent}");
        }
        catch (Exception ex) when (ex is not GatewayDomainException)
        {
            _logger.LogError(ex, "Erro de rede ao solicitar ConnectToken para o usuário {UserId}", userId);
            throw new GatewayDownstreamException(GatewayConstants.Downstream.MercadoPagoIntegrationServiceName, "MercadoPagoIntegration indisponível.", ex);
        }
    }

    public async Task<GatewaySyncResultDto> TriggerSyncAsync(string userId, string itemId, string? accountId = null, CancellationToken ct = default)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/v1/mercadopago/sync", new { UserId = userId, ItemId = itemId, AccountId = accountId }, ct);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<GatewaySyncResultDto>(cancellationToken: ct);
                return result ?? new GatewaySyncResultDto(Guid.NewGuid(), "Accepted", 0, DateTime.UtcNow);
            }

            var errorContent = await response.Content.ReadAsStringAsync(ct);
            _logger.LogWarning("Falha ao sincronizar Mercado Pago downstream: {Status} - {Error}", response.StatusCode, errorContent);
            throw new GatewayDownstreamException(GatewayConstants.Downstream.MercadoPagoIntegrationServiceName, $"Status {(int)response.StatusCode}: {errorContent}");
        }
        catch (Exception ex) when (ex is not GatewayDomainException)
        {
            _logger.LogError(ex, "Erro de rede ao conectar com MercadoPagoIntegration para o usuário {UserId}", userId);
            throw new GatewayDownstreamException(GatewayConstants.Downstream.MercadoPagoIntegrationServiceName, "MercadoPagoIntegration indisponível.", ex);
        }
    }
}
