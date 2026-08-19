using FinanceHub.PluggyIntegration.Application.Commands.SyncAllPluggyAccounts;
using FinanceHub.PluggyIntegration.Application.DTOs;

namespace FinanceHub.PluggyIntegration.Application.Commands.SyncSinglePluggyItem;

public interface ISyncSinglePluggyItemCommandHandler
{
    Task<SyncPluggySummaryDto> HandleAsync(SyncSinglePluggyItemCommand command, CancellationToken cancellationToken = default);
}
