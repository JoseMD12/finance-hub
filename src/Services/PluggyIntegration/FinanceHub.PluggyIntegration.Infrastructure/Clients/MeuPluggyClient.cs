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

public sealed class MeuPluggyClient(
    HttpClient httpClient,
    IOptions<PluggyOptions> options,
    ILogger<MeuPluggyClient> logger) : IMeuPluggyClient
{
    private readonly PluggyOptions _options = options.Value;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private static void EnsureAuthorizationHeader(HttpRequestMessage request, string pluggyAccessToken)
    {
        if (string.IsNullOrWhiteSpace(pluggyAccessToken))
        {
            throw new NullOrEmptyPluggyAccessTokenDomainException();
        }

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", pluggyAccessToken);
    }

    public async Task<IReadOnlyList<PluggyItemDto>> GetItemsAsync(string pluggyAccessToken, CancellationToken cancellationToken = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, PluggyConstants.ItemsEndpoint);
        EnsureAuthorizationHeader(request, pluggyAccessToken);

        var response = await SendWithResilienceAsync(request, cancellationToken);
        var content = await response.Content.ReadAsStringAsync(cancellationToken);

        var items = JsonSerializer.Deserialize<List<PluggyItemDto>>(content, JsonOptions);
        return items ?? [];
    }

    public async Task<IReadOnlyList<PluggyAccountDto>> GetAllAccountsAsync(string pluggyAccessToken, CancellationToken cancellationToken = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, PluggyConstants.AccountsEndpoint);
        EnsureAuthorizationHeader(request, pluggyAccessToken);

        var response = await SendWithResilienceAsync(request, cancellationToken);
        var content = await response.Content.ReadAsStringAsync(cancellationToken);

        var accounts = JsonSerializer.Deserialize<List<PluggyAccountDto>>(content, JsonOptions);
        return accounts ?? [];
    }

    public async Task<IReadOnlyList<PluggyAccountDto>> GetAccountsByItemIdAsync(string itemId, string pluggyAccessToken, CancellationToken cancellationToken = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"{PluggyConstants.AccountsEndpoint}?itemId={Uri.EscapeDataString(itemId)}");
        EnsureAuthorizationHeader(request, pluggyAccessToken);

        var response = await SendWithResilienceAsync(request, cancellationToken);
        var content = await response.Content.ReadAsStringAsync(cancellationToken);

        var accounts = JsonSerializer.Deserialize<List<PluggyAccountDto>>(content, JsonOptions);
        return accounts ?? [];
    }

    public async Task<IReadOnlyList<PluggyTransactionDto>> GetAllTransactionsAsync(string pluggyAccessToken, CancellationToken cancellationToken = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, PluggyConstants.TransactionsEndpoint);
        EnsureAuthorizationHeader(request, pluggyAccessToken);

        var response = await SendWithResilienceAsync(request, cancellationToken);
        var content = await response.Content.ReadAsStringAsync(cancellationToken);

        var txs = JsonSerializer.Deserialize<List<PluggyTransactionDto>>(content, JsonOptions);
        return txs ?? [];
    }

    public async Task<IReadOnlyList<PluggyTransactionDto>> GetTransactionsByAccountIdAsync(string accountId, string pluggyAccessToken, CancellationToken cancellationToken = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"{PluggyConstants.TransactionsEndpoint}?accountId={Uri.EscapeDataString(accountId)}");
        EnsureAuthorizationHeader(request, pluggyAccessToken);

        var response = await SendWithResilienceAsync(request, cancellationToken);
        var content = await response.Content.ReadAsStringAsync(cancellationToken);

        var txs = JsonSerializer.Deserialize<List<PluggyTransactionDto>>(content, JsonOptions);
        return txs ?? [];
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
