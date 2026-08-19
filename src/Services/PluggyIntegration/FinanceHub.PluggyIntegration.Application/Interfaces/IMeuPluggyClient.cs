using FinanceHub.PluggyIntegration.Application.DTOs;

namespace FinanceHub.PluggyIntegration.Application.Interfaces;

public interface IMeuPluggyClient
{
    Task<IReadOnlyList<PluggyItemDto>> GetItemsAsync(string pluggyAccessToken, CancellationToken cancellationToken = default);
    Task<PluggyItemDto> UpdateItemAsync(string itemId, string pluggyAccessToken, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PluggyAccountDto>> GetAllAccountsAsync(string pluggyAccessToken, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PluggyAccountDto>> GetAccountsByItemIdAsync(string itemId, string pluggyAccessToken, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PluggyTransactionDto>> GetAllTransactionsAsync(string pluggyAccessToken, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PluggyTransactionDto>> GetTransactionsByAccountIdAsync(string accountId, string pluggyAccessToken, CancellationToken cancellationToken = default);
}
