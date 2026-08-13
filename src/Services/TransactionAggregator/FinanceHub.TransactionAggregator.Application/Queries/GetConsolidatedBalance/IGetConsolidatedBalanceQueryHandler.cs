using System.Threading;
using System.Threading.Tasks;
using FinanceHub.TransactionAggregator.Application.DTOs;

namespace FinanceHub.TransactionAggregator.Application.Queries.GetConsolidatedBalance;

public interface IGetConsolidatedBalanceQueryHandler
{
    Task<ConsolidatedBalanceDto> Handle(GetConsolidatedBalanceQuery query, CancellationToken cancellationToken);
}
