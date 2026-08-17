using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using FinanceHub.PluggyIntegration.Application.DTOs;
using FinanceHub.PluggyIntegration.Application.Interfaces;
using FinanceHub.PluggyIntegration.Domain.Constants;
using FinanceHub.PluggyIntegration.Domain.Exceptions;
using FinanceHub.PluggyIntegration.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FinanceHub.PluggyIntegration.Infrastructure.Clients;

public class MeuPluggyClient : IMeuPluggyClient
{
    private readonly HttpClient _httpClient;
    private readonly PluggyOptions _options;
    private readonly ILogger<MeuPluggyClient> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public MeuPluggyClient(
        HttpClient httpClient,
        IOptions<PluggyOptions> options,
        ILogger<MeuPluggyClient> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    private void EnsureAuthorizationHeader(HttpRequestMessage request)
    {
        var token = !string.IsNullOrWhiteSpace(_options.UserToken)
            ? _options.UserToken
            : Environment.GetEnvironmentVariable("PLUGGY_USER_TOKEN") ?? string.Empty;

        if (string.IsNullOrWhiteSpace(token))
        {
            throw new PluggySessionExpiredDomainException("A variável de ambiente PLUGGY_USER_TOKEN não foi informada.");
        }

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    public async Task<IReadOnlyList<PluggyItemDto>> GetItemsAsync(CancellationToken cancellationToken = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, PluggyConstants.ItemsEndpoint);
        EnsureAuthorizationHeader(request);

        var response = await SendWithResilienceAsync(request, cancellationToken);
        var content = await response.Content.ReadAsStringAsync(cancellationToken);

        var items = JsonSerializer.Deserialize<List<PluggyItemDto>>(content, JsonOptions);
        return items ?? [];
    }

    public async Task<IReadOnlyList<PluggyAccountDto>> GetAccountsByItemIdAsync(string itemId, CancellationToken cancellationToken = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"{PluggyConstants.AccountsEndpoint}?itemId={Uri.EscapeDataString(itemId)}");
        EnsureAuthorizationHeader(request);

        var response = await SendWithResilienceAsync(request, cancellationToken);
        var content = await response.Content.ReadAsStringAsync(cancellationToken);

        var accounts = JsonSerializer.Deserialize<List<PluggyAccountDto>>(content, JsonOptions);
        return accounts ?? [];
    }

    public async Task<IReadOnlyList<PluggyTransactionDto>> GetTransactionsByAccountIdAsync(string accountId, CancellationToken cancellationToken = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"{PluggyConstants.TransactionsEndpoint}?accountId={Uri.EscapeDataString(accountId)}");
        EnsureAuthorizationHeader(request);

        var response = await SendWithResilienceAsync(request, cancellationToken);
        var content = await response.Content.ReadAsStringAsync(cancellationToken);

        var txs = JsonSerializer.Deserialize<List<PluggyTransactionDto>>(content, JsonOptions);
        return txs ?? [];
    }

    private async Task<HttpResponseMessage> SendWithResilienceAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        try
        {
            var response = await _httpClient.SendAsync(request, cancellationToken);

            if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
            {
                _logger.LogWarning("API da Pluggy retornou {StatusCode}. A sessão expirou.", response.StatusCode);
                throw new PluggySessionExpiredDomainException("Token de sessão inválido ou expirado.");
            }

            if (response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                _logger.LogWarning("Rate limit da API da Pluggy excedido.");
                throw new PluggyRateLimitDomainException();
            }

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Erro na comunicação com Pluggy API: {StatusCode} - {Error}", response.StatusCode, errorBody);
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
            _logger.LogError(ex, "Falha de rede ou conectividade com a API da Pluggy.");
            throw new PluggyApiCommunicationDomainException("Não foi possível conectar à API da Pluggy.", ex);
        }
    }
}
