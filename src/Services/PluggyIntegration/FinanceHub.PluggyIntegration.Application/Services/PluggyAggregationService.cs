using FinanceHub.PluggyIntegration.Application.DTOs;
using FinanceHub.PluggyIntegration.Application.Interfaces;

namespace FinanceHub.PluggyIntegration.Application.Services;

public sealed class PluggyAggregationService(IMeuPluggyClient pluggyClient) : IPluggyAggregationService
{
    public async Task<IReadOnlyList<PluggyAccountDto>> FetchAllAccountsAsync(string pluggyAccessToken, CancellationToken cancellationToken = default)
    {
        var items = await pluggyClient.GetItemsAsync(pluggyAccessToken, cancellationToken);
        if (items.Count == 0)
        {
            return Array.Empty<PluggyAccountDto>();
        }

        var tasks = items.Select(item => pluggyClient.GetAccountsByItemIdAsync(item.Id, pluggyAccessToken, cancellationToken));
        var results = await Task.WhenAll(tasks);
        return results.SelectMany(x => x).ToList();
    }

    public async Task<IReadOnlyList<PluggyTransactionDto>> FetchAllTransactionsAsync(string pluggyAccessToken, CancellationToken cancellationToken = default)
    {
        var accounts = await FetchAllAccountsAsync(pluggyAccessToken, cancellationToken);
        if (accounts.Count == 0)
        {
            return Array.Empty<PluggyTransactionDto>();
        }

        var tasks = accounts.Select(acc => pluggyClient.GetTransactionsByAccountIdAsync(acc.Id, pluggyAccessToken, cancellationToken));
        var results = await Task.WhenAll(tasks);
        return results.SelectMany(x => x).ToList();
    }
}
