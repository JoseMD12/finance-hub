using FinanceHub.PluggyIntegration.Application.DTOs;
using FinanceHub.PluggyIntegration.Application.Interfaces;
using FinanceHub.PluggyIntegration.Domain.Constants;

namespace FinanceHub.PluggyIntegration.Infrastructure.Clients;

public sealed class MeuPluggyClient(IPluggyHttpExecutor executor) : IMeuPluggyClient
{
    public Task<IReadOnlyList<PluggyItemDto>> GetItemsAsync(string pluggyAccessToken, CancellationToken cancellationToken = default)
        => executor.GetAsync<IReadOnlyList<PluggyItemDto>>(PluggyConstants.ItemsEndpoint, pluggyAccessToken, cancellationToken);

    public Task<IReadOnlyList<PluggyAccountDto>> GetAccountsByItemIdAsync(string itemId, string pluggyAccessToken, CancellationToken cancellationToken = default)
        => executor.GetAsync<IReadOnlyList<PluggyAccountDto>>($"{PluggyConstants.AccountsEndpoint}?itemId={Uri.EscapeDataString(itemId)}", pluggyAccessToken, cancellationToken);

    public Task<IReadOnlyList<PluggyTransactionDto>> GetTransactionsByAccountIdAsync(string accountId, string pluggyAccessToken, CancellationToken cancellationToken = default)
        => executor.GetAsync<IReadOnlyList<PluggyTransactionDto>>($"{PluggyConstants.TransactionsEndpoint}?accountId={Uri.EscapeDataString(accountId)}", pluggyAccessToken, cancellationToken);
}
