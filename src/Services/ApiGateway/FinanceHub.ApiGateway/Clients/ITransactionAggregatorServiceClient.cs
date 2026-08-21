using FinanceHub.ApiGateway.DTOs;

namespace FinanceHub.ApiGateway.Clients;

public interface ITransactionAggregatorServiceClient
{
    Task<GatewayConsolidatedBalanceDto> GetConsolidatedBalanceAsync(string userId, CancellationToken ct = default);
    Task<PagedGatewayTransactionsDto> GetTransactionsAsync(GatewayTransactionFilterDto filter, CancellationToken ct = default);
    Task<IEnumerable<GatewayCategoryDto>> GetCategoriesAsync(CancellationToken ct = default);
    Task CategorizeTransactionAsync(Guid transactionId, string userId, Guid categoryId, bool createCustomRule = false, CancellationToken ct = default);
    Task<bool> HealthCheckAsync(CancellationToken ct = default);
}
