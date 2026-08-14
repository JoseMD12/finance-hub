using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;

using FinanceHub.ApiGateway.Clients.Extensions;
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
        using var request = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/consents/user/{userId}");
        var consents = await _httpClient.SendAndDeserializeAsync<IEnumerable<GatewayConsentDto>>(request, ServiceName, _logger, ct);
        return consents ?? Enumerable.Empty<GatewayConsentDto>();
    }

    public async Task<Guid> CreateConsentAsync(string userId, string institutionId, string externalConsentId, CancellationToken ct = default)
    {
        var payload = new { UserId = userId, InstitutionId = institutionId, ExternalConsentId = externalConsentId };
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/consents")
        {
            Content = JsonContent.Create(payload)
        };

        var result = await _httpClient.SendAndDeserializeAsync<CreateConsentResponse>(request, ServiceName, _logger, ct);
        return result?.ConsentId ?? Guid.Empty;
    }

    public async Task<GatewayConsentDto> AuthorizeConsentAsync(Guid consentId, string authCode, string redirectUri, CancellationToken ct = default)
    {
        var payload = new { AuthCode = authCode, RedirectUri = redirectUri };
        using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/consents/{consentId}/authorize")
        {
            Content = JsonContent.Create(payload)
        };

        var consent = await _httpClient.SendAndDeserializeAsync<GatewayConsentDto>(request, ServiceName, _logger, ct);
        if (consent == null)
        {
            throw new GatewayDownstreamException(ServiceName, "Resposta nula retornada pela autorização de consentimento.");
        }

        return consent;
    }

    public async Task RevokeConsentAsync(Guid consentId, CancellationToken ct = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Delete, $"/api/v1/consents/{consentId}");
        await _httpClient.SendOrThrowAsync(request, ServiceName, _logger, HttpStatusCode.NotFound, ct);
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
