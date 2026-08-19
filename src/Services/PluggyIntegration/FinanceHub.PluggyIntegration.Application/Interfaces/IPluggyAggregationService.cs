using FinanceHub.PluggyIntegration.Application.DTOs;

namespace FinanceHub.PluggyIntegration.Application.Interfaces;

public interface IPluggyAggregationService
{
    Task<IReadOnlyList<PluggyAccountDto>> FetchAllAccountsAsync(string pluggyAccessToken, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PluggyTransactionDto>> FetchAllTransactionsAsync(string pluggyAccessToken, CancellationToken cancellationToken = default);
}
