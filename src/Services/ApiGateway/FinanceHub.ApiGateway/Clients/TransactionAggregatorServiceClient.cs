using System.Net.Http.Json;

using FinanceHub.ApiGateway.DTOs;
using FinanceHub.ApiGateway.Exceptions;

namespace FinanceHub.ApiGateway.Clients;

public class TransactionAggregatorServiceClient : ITransactionAggregatorServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<TransactionAggregatorServiceClient> _logger;

    public TransactionAggregatorServiceClient(HttpClient httpClient, ILogger<TransactionAggregatorServiceClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<GatewayConsolidatedBalanceDto> GetConsolidatedBalanceAsync(string userId, CancellationToken ct = default)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/v1/transactions/balances/user/{userId}", ct);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(ct);
                throw new GatewayDownstreamException("TransactionAggregator", $"Falha ao buscar saldo consolidado para userId '{userId}'. Status: {response.StatusCode}. Detalhes: {errorContent}");
            }

            var balance = await response.Content.ReadFromJsonAsync<GatewayConsolidatedBalanceDto>(cancellationToken: ct);
            return balance ?? new GatewayConsolidatedBalanceDto(userId, 0m, Enumerable.Empty<GatewayAccountBalanceDto>());
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Erro de conexão ao chamar TransactionAggregator GetConsolidatedBalanceAsync para userId {UserId}", userId);
            throw new GatewayDownstreamException("TransactionAggregator", ex.Message, ex);
        }
    }

    public async Task<IEnumerable<GatewayTransactionDto>> GetTransactionsAsync(string userId, int page = 1, int pageSize = 20, CancellationToken ct = default)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/v1/transactions?userId={userId}&page={page}&pageSize={pageSize}", ct);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(ct);
                throw new GatewayDownstreamException("TransactionAggregator", $"Falha ao buscar transações para userId '{userId}'. Status: {response.StatusCode}. Detalhes: {errorContent}");
            }

            var transactions = await response.Content.ReadFromJsonAsync<IEnumerable<GatewayTransactionDto>>(cancellationToken: ct);
            return transactions ?? Enumerable.Empty<GatewayTransactionDto>();
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Erro de conexão ao buscar transações no TransactionAggregator");
            throw new GatewayDownstreamException("TransactionAggregator", ex.Message, ex);
        }
    }

    public async Task CategorizeTransactionAsync(Guid transactionId, string userId, Guid categoryId, bool createCustomRule = false, CancellationToken ct = default)
    {
        try
        {
            var payload = new { UserId = userId, NewCategoryId = categoryId, CreateCustomRule = createCustomRule };
            var response = await _httpClient.PatchAsJsonAsync($"/api/v1/transactions/{transactionId}/categorize", payload, ct);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(ct);
                throw new GatewayDownstreamException("TransactionAggregator", $"Falha ao categorizar transação '{transactionId}'. Status: {response.StatusCode}. Detalhes: {errorContent}");
            }
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Erro de conexão ao categorizar transação {TransactionId}", transactionId);
            throw new GatewayDownstreamException("TransactionAggregator", ex.Message, ex);
        }
    }

    public async Task<bool> HealthCheckAsync(CancellationToken ct = default)
    {
        try
        {
            var response = await _httpClient.GetAsync("/health", ct);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
}
