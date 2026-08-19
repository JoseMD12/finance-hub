using FinanceHub.PluggyIntegration.Application.DTOs;
using FinanceHub.PluggyIntegration.Application.Interfaces;
using FinanceHub.PluggyIntegration.Domain.Exceptions;

namespace FinanceHub.PluggyIntegration.Application.Queries.GetPluggyAccounts;

public sealed class GetPluggyAccountsQueryHandler(
    IMeuPluggyClient pluggyClient) : IGetPluggyAccountsQueryHandler
{
    public async Task<IReadOnlyList<PluggyConnectedAccountDto>> HandleAsync(
        GetPluggyAccountsQuery query,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query.PluggyAccessToken))
        {
            throw new NullOrEmptyPluggyAccessTokenDomainException();
        }

        var items = await pluggyClient.GetItemsAsync(query.PluggyAccessToken, cancellationToken);
        var accounts = new List<PluggyConnectedAccountDto>();

        foreach (var item in items)
        {
            var itemAccounts = await pluggyClient.GetAccountsByItemIdAsync(
                item.Id,
                query.PluggyAccessToken,
                cancellationToken);

            accounts.AddRange(itemAccounts.Select(account => new PluggyConnectedAccountDto(
                item.Id,
                item.Connector.Name,
                account.Name,
                account.Type,
                account.Subtype,
                account.Balance,
                account.CreditData)));
        }

        return accounts;
    }
}
