using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;

using FinanceHub.ApiGateway.DTOs;
using FinanceHub.ApiGateway.Exceptions;

using Microsoft.Extensions.Logging;

namespace FinanceHub.ApiGateway.Clients;

public class AuthConsentServiceClient : IAuthConsentServiceClient
{
    private const string ServiceName = GatewayConstants.Downstream.AuthConsentServiceName;

    private readonly HttpClient _httpClient;
    private readonly ILogger<AuthConsentServiceClient> _logger;

    public AuthConsentServiceClient(HttpClient httpClient, ILogger<AuthConsentServiceClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<IEnumerable<GatewayConsentDto>> GetConsentsByUserIdAsync(string userId, CancellationToken ct = default)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/v1/consents/user/{userId}", ct);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(ct);
                throw new GatewayDownstreamException(ServiceName, $"Falha ao buscar consentimentos para o usuário '{userId}'. Status: {response.StatusCode}. Detalhes: {errorContent}");
            }

            var consents = await response.Content.ReadFromJsonAsync<IEnumerable<GatewayConsentDto>>(cancellationToken: ct);
            return consents ?? Enumerable.Empty<GatewayConsentDto>();
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Erro de conexão ao chamar AuthConsent GetConsentsByUserIdAsync para userId {UserId}", userId);
            throw new GatewayDownstreamException(ServiceName, ex.Message, ex);
        }
    }

    public async Task<Guid> CreateConsentAsync(string userId, string institutionId, string externalConsentId, CancellationToken ct = default)
    {
        try
        {
            var payload = new { UserId = userId, InstitutionId = institutionId, ExternalConsentId = externalConsentId };
            var response = await _httpClient.PostAsJsonAsync("/api/v1/consents", payload, ct);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(ct);
                throw new GatewayDownstreamException(ServiceName, $"Falha ao criar consentimento. Status: {response.StatusCode}. Detalhes: {errorContent}");
            }

            var result = await response.Content.ReadFromJsonAsync<CreateConsentResponse>(cancellationToken: ct);
            return result?.ConsentId ?? Guid.Empty;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Erro de conexão ao chamar AuthConsent CreateConsentAsync");
            throw new GatewayDownstreamException(ServiceName, ex.Message, ex);
        }
    }

    public async Task<GatewayConsentDto> AuthorizeConsentAsync(Guid consentId, string authCode, string redirectUri, CancellationToken ct = default)
    {
        try
        {
            var payload = new { AuthCode = authCode, RedirectUri = redirectUri };
            var response = await _httpClient.PostAsJsonAsync($"/api/v1/consents/{consentId}/authorize", payload, ct);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(ct);
                throw new GatewayDownstreamException(ServiceName, $"Falha ao autorizar consentimento '{consentId}'. Status: {response.StatusCode}. Detalhes: {errorContent}");
            }

            var consent = await response.Content.ReadFromJsonAsync<GatewayConsentDto>(cancellationToken: ct);
            if (consent == null)
            {
                throw new GatewayDownstreamException(ServiceName, "Resposta nula retornada pela autorização de consentimento.");
            }

            return consent;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Erro de conexão ao autorizar consentimento {ConsentId}", consentId);
            throw new GatewayDownstreamException(ServiceName, ex.Message, ex);
        }
    }

    public async Task RevokeConsentAsync(Guid consentId, CancellationToken ct = default)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"/api/v1/consents/{consentId}", ct);

            if (!response.IsSuccessStatusCode && response.StatusCode != HttpStatusCode.NotFound)
            {
                var errorContent = await response.Content.ReadAsStringAsync(ct);
                throw new GatewayDownstreamException(ServiceName, $"Falha ao revogar consentimento '{consentId}'. Status: {response.StatusCode}. Detalhes: {errorContent}");
            }
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Erro de conexão ao revogar consentimento {ConsentId}", consentId);
            throw new GatewayDownstreamException(ServiceName, ex.Message, ex);
        }
    }

    public async Task<bool> HealthCheckAsync(CancellationToken ct = default)
    {
        try
        {
            var response = await _httpClient.GetAsync("/health", ct);
            return response.IsSuccessStatusCode;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Health check falhou para o serviço AuthConsent");
            return false;
        }
        catch (OperationCanceledException)
        {
            return false;
        }
    }

    private sealed record CreateConsentResponse(Guid ConsentId);
}
