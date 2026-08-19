using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FinanceHub.PluggyIntegration.Application.DTOs;
using FinanceHub.PluggyIntegration.Application.Interfaces;
using FinanceHub.PluggyIntegration.Domain.Exceptions;

namespace FinanceHub.PluggyIntegration.Application.Queries.GetPluggyItems;

public sealed class GetPluggyItemsQueryHandler : IGetPluggyItemsQueryHandler
{
    private readonly IMeuPluggyClient _pluggyClient;

    public GetPluggyItemsQueryHandler(IMeuPluggyClient pluggyClient)
    {
        _pluggyClient = pluggyClient;
    }

    public async Task<IReadOnlyList<PluggyItemDto>> HandleAsync(GetPluggyItemsQuery query, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query.PluggyAccessToken))
        {
            throw new NullOrEmptyPluggyAccessTokenDomainException();
        }

        var items = await _pluggyClient.GetItemsAsync(query.PluggyAccessToken, cancellationToken);
        var enrichedList = new List<PluggyItemDto>(items.Count);

        foreach (var item in items)
        {
            try
            {
                var accounts = await _pluggyClient.GetAccountsByItemIdAsync(item.Id, query.PluggyAccessToken, cancellationToken);
                decimal totalBalance = 0m;
                decimal totalCredit = 0m;
                foreach (var acc in accounts)
                {
                    if (IsCreditCardAccount(acc))
                    {
                        totalCredit += GetUsedCredit(acc);
                    }
                    else
                    {
                        totalBalance += acc.Balance;
                    }
                }

                enrichedList.Add(item with
                {
                    TotalBalance = totalBalance,
                    AccountsCount = accounts.Count,
                    TotalCredit = totalCredit
                });
            }
            catch
            {
                enrichedList.Add(item);
            }
        }

        return enrichedList;
    }

    private static bool IsCreditCardAccount(PluggyAccountDto account) =>
        account.Type == FinanceHub.PluggyIntegration.Domain.Constants.PluggyConstants.AccountTypes.Credit ||
        account.Subtype == FinanceHub.PluggyIntegration.Domain.Constants.PluggyConstants.AccountSubtypes.CreditCard;

    private static decimal GetUsedCredit(PluggyAccountDto account)
    {
        if (account.CreditData?.CreditLimit is decimal creditLimit &&
            account.CreditData.AvailableCreditLimit is decimal availableCreditLimit)
        {
            return Math.Max(0m, creditLimit - availableCreditLimit);
        }

        return Math.Abs(account.Balance);
    }
}
