using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FinanceHub.PluggyIntegration.Application.DTOs;

namespace FinanceHub.PluggyIntegration.Application.Queries.GetPluggyItems;

public interface IGetPluggyItemsQueryHandler
{
    Task<IReadOnlyList<PluggyItemDto>> HandleAsync(GetPluggyItemsQuery query, CancellationToken cancellationToken = default);
}
