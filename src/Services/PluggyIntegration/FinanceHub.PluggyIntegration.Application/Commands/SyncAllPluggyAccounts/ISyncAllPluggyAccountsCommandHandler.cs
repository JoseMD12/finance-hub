using FinanceHub.PluggyIntegration.Application.DTOs;

namespace FinanceHub.PluggyIntegration.Application.Commands.SyncAllPluggyAccounts;

public interface ISyncAllPluggyAccountsCommandHandler
{
    Task<SyncPluggySummaryDto> HandleAsync(SyncAllPluggyAccountsCommand command, CancellationToken cancellationToken = default);
}
