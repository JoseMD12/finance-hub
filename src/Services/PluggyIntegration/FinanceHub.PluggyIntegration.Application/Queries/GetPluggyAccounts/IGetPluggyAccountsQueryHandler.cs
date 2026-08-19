using FinanceHub.PluggyIntegration.Application.DTOs;

namespace FinanceHub.PluggyIntegration.Application.Queries.GetPluggyAccounts;

public interface IGetPluggyAccountsQueryHandler
{
    Task<IReadOnlyList<PluggyConnectedAccountDto>> HandleAsync(
        GetPluggyAccountsQuery query,
        CancellationToken cancellationToken = default);
}
