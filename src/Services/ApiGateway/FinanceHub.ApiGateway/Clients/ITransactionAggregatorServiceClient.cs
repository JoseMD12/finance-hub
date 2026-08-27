using FinanceHub.ApiGateway.DTOs;

namespace FinanceHub.ApiGateway.Clients;

public interface ITransactionAggregatorServiceClient
{
    Task<GatewayConsolidatedBalanceDto> GetConsolidatedBalanceAsync(string userId, CancellationToken ct = default);
    Task<PagedGatewayTransactionsDto> GetTransactionsAsync(GatewayTransactionFilterDto filter, CancellationToken ct = default);
    Task<IEnumerable<GatewayCategoryDto>> GetCategoriesAsync(CancellationToken ct = default);
    Task CategorizeTransactionAsync(Guid transactionId, string userId, Guid categoryId, bool createCustomRule = false, bool applyToPastTransactions = false, CancellationToken ct = default);
    Task ToggleTransactionNeutralityAsync(Guid transactionId, string userId, bool isIgnoredInTotals, string? reason = null, CancellationToken ct = default);
    Task ToggleBillPaymentAsync(Guid transactionId, string userId, bool isBillPayment, CancellationToken ct = default);
    Task UpdateTransactionNotesAsync(Guid transactionId, string userId, string? notes, CancellationToken ct = default);
    Task<bool> HealthCheckAsync(CancellationToken ct = default);
}
