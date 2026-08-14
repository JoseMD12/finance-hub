using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using FinanceHub.ApiGateway.Exceptions;
using Microsoft.Extensions.Logging;

namespace FinanceHub.ApiGateway.Clients.Extensions;

public static class HttpClientDownstreamExtensions
{
    public static async Task<T?> SendAndDeserializeAsync<T>(
        this HttpClient httpClient,
        HttpRequestMessage request,
        string serviceName,
        ILogger logger,
        CancellationToken ct = default)
    {
        try
        {
            var response = await httpClient.SendAsync(request, ct);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(ct);
                throw new GatewayDownstreamException(serviceName, $"Falha ao chamar serviço downstream. Status: {response.StatusCode}. Detalhes: {errorContent}");
            }

            return await response.Content.ReadFromJsonAsync<T>(cancellationToken: ct);
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Erro de conexão ao chamar serviço downstream '{ServiceName}'", serviceName);
            throw new GatewayDownstreamException(serviceName, ex.Message, ex);
        }
    }

    public static async Task SendOrThrowAsync(
        this HttpClient httpClient,
        HttpRequestMessage request,
        string serviceName,
        ILogger logger,
        HttpStatusCode? ignoredStatusCode = null,
        CancellationToken ct = default)
    {
        try
        {
            var response = await httpClient.SendAsync(request, ct);

            if (!response.IsSuccessStatusCode && (ignoredStatusCode == null || response.StatusCode != ignoredStatusCode))
            {
                var errorContent = await response.Content.ReadAsStringAsync(ct);
                throw new GatewayDownstreamException(serviceName, $"Falha ao executar operação downstream. Status: {response.StatusCode}. Detalhes: {errorContent}");
            }
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Erro de conexão ao chamar serviço downstream '{ServiceName}'", serviceName);
            throw new GatewayDownstreamException(serviceName, ex.Message, ex);
        }
    }
}
