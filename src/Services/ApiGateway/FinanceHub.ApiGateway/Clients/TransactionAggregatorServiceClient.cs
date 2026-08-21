using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using FinanceHub.ApiGateway.Clients.Extensions;
using FinanceHub.ApiGateway.DTOs;
using FinanceHub.ApiGateway.Exceptions;
using Microsoft.Extensions.Logging;

namespace FinanceHub.ApiGateway.Clients;

public class TransactionAggregatorServiceClient : ITransactionAggregatorServiceClient
{
    private const string ServiceName = GatewayConstants.Downstream.TransactionAggregatorServiceName;

    private readonly HttpClient _httpClient;
    private readonly ILogger<TransactionAggregatorServiceClient> _logger;

    public TransactionAggregatorServiceClient(HttpClient httpClient, ILogger<TransactionAggregatorServiceClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<GatewayConsolidatedBalanceDto> GetConsolidatedBalanceAsync(string userId, CancellationToken ct = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/transactions/balances/user/{userId}");
        var balance = await _httpClient.SendAndDeserializeAsync<GatewayConsolidatedBalanceDto>(request, ServiceName, _logger, ct);
        return balance ?? new GatewayConsolidatedBalanceDto(userId, 0m, Enumerable.Empty<GatewayAccountBalanceDto>());
    }

    public async Task<PagedGatewayTransactionsDto> GetTransactionsAsync(GatewayTransactionFilterDto filter, CancellationToken ct = default)
    {
        var queryParams = new List<string>
        {
            $"userId={Uri.EscapeDataString(filter.UserId)}",
            $"page={filter.Page}",
            $"pageSize={filter.PageSize}"
        };

        if (filter.StartDate.HasValue)
        {
            queryParams.Add($"startDate={filter.StartDate.Value:O}");
        }

        if (filter.EndDate.HasValue)
        {
            queryParams.Add($"endDate={filter.EndDate.Value:O}");
        }

        if (!string.IsNullOrWhiteSpace(filter.InstitutionId))
        {
            queryParams.Add($"institutionId={Uri.EscapeDataString(filter.InstitutionId)}");
        }

        if (filter.CategoryId.HasValue)
        {
            queryParams.Add($"categoryId={filter.CategoryId.Value}");
        }

        if (!string.IsNullOrWhiteSpace(filter.Type))
        {
            queryParams.Add($"type={Uri.EscapeDataString(filter.Type)}");
        }

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            queryParams.Add($"search={Uri.EscapeDataString(filter.Search)}");
        }

        var queryString = string.Join("&", queryParams);
        using var request = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/transactions?{queryString}");
        var response = await _httpClient.SendAndDeserializeAsync<PagedGatewayTransactionsDto>(request, ServiceName, _logger, ct);

        return response ?? new PagedGatewayTransactionsDto(
            Enumerable.Empty<GatewayTransactionDto>(),
            new GatewayTransactionSummaryDto(0m, 0m, 0m, 0),
            filter.Page,
            filter.PageSize,
            0,
            0);
    }

    public async Task<IEnumerable<GatewayCategoryDto>> GetCategoriesAsync(CancellationToken ct = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/categories");
        var categories = await _httpClient.SendAndDeserializeAsync<IEnumerable<GatewayCategoryDto>>(request, ServiceName, _logger, ct);
        return categories ?? Enumerable.Empty<GatewayCategoryDto>();
    }

    public async Task CategorizeTransactionAsync(Guid transactionId, string userId, Guid categoryId, bool createCustomRule = false, CancellationToken ct = default)
    {
        var payload = new { UserId = userId, NewCategoryId = categoryId, CreateCustomRule = createCustomRule };
        using var request = new HttpRequestMessage(HttpMethod.Patch, $"/api/v1/transactions/{transactionId}/categorize")
        {
            Content = JsonContent.Create(payload)
        };

        await _httpClient.SendOrThrowAsync(request, ServiceName, _logger, null, ct);
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
            _logger.LogWarning(ex, "Health check falhou para o serviço TransactionAggregator");
            return false;
        }
        catch (OperationCanceledException)
        {
            return false;
        }
    }
}
