using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FinanceHub.PluggyIntegration.Domain.Exceptions;
using Microsoft.Extensions.Logging;

namespace FinanceHub.PluggyIntegration.Infrastructure.Clients;

public sealed class PluggyHttpExecutor(
    HttpClient httpClient,
    ILogger<PluggyHttpExecutor> logger) : IPluggyHttpExecutor
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<TResponse> GetAsync<TResponse>(string endpoint, string accessToken, CancellationToken cancellationToken = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, endpoint);
        EnsureAuthorizationHeader(request, accessToken);

        var response = await SendWithResilienceAsync(request, cancellationToken);
        var content = await response.Content.ReadAsStringAsync(cancellationToken);

        var result = JsonSerializer.Deserialize<TResponse>(content, JsonOptions);
        return result ?? throw new PluggyApiCommunicationDomainException("A API da Pluggy retornou uma resposta vazia.");
    }

    public async Task<TResponse> PatchAsync<TResponse>(string endpoint, object body, string accessToken, CancellationToken cancellationToken = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Patch, endpoint)
        {
            Content = JsonContent.Create(body)
        };
        EnsureAuthorizationHeader(request, accessToken);

        var response = await SendWithResilienceAsync(request, cancellationToken);
        var content = await response.Content.ReadAsStringAsync(cancellationToken);

        var result = JsonSerializer.Deserialize<TResponse>(content, JsonOptions);
        return result ?? throw new PluggyApiCommunicationDomainException("A API da Pluggy retornou uma resposta vazia.");
    }

    private static void EnsureAuthorizationHeader(HttpRequestMessage request, string accessToken)
    {
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            throw new NullOrEmptyPluggyAccessTokenDomainException();
        }

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
    }

    private async Task<HttpResponseMessage> SendWithResilienceAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        try
        {
            var response = await httpClient.SendAsync(request, cancellationToken);

            if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
            {
                logger.LogWarning("API da Pluggy retornou {StatusCode}. A sessão expirou.", response.StatusCode);
                throw new PluggySessionExpiredDomainException("Token de sessão inválido ou expirado.");
            }

            if (response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                logger.LogWarning("Rate limit da API da Pluggy excedido.");
                throw new PluggyRateLimitDomainException();
            }

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                logger.LogError("Erro na comunicação com Pluggy API: {StatusCode} - {Error}", response.StatusCode, errorBody);
                throw new PluggyApiCommunicationDomainException($"Erro HTTP {(int)response.StatusCode} ao comunicar com a API da Pluggy: {errorBody}");
            }

            return response;
        }
        catch (DomainException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Falha de rede ou conectividade com a API da Pluggy.");
            throw new PluggyApiCommunicationDomainException("Não foi possível conectar à API da Pluggy.", ex);
        }
    }
}
