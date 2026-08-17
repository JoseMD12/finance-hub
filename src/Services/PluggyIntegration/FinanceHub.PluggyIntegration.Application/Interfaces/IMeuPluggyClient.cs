using FinanceHub.PluggyIntegration.Application.DTOs;

namespace FinanceHub.PluggyIntegration.Application.Interfaces;

public interface IMeuPluggyClient
{
    Task<IReadOnlyList<PluggyItemDto>> GetItemsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PluggyAccountDto>> GetAccountsByItemIdAsync(string itemId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PluggyTransactionDto>> GetTransactionsByAccountIdAsync(string accountId, CancellationToken cancellationToken = default);
}
