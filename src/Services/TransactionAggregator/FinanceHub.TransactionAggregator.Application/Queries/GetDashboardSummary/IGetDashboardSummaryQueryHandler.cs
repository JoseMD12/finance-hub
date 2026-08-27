using System.Threading;
using System.Threading.Tasks;
using FinanceHub.TransactionAggregator.Application.DTOs;

namespace FinanceHub.TransactionAggregator.Application.Queries.GetDashboardSummary;

public interface IGetDashboardSummaryQueryHandler
{
    Task<DashboardSummaryDto> Handle(GetDashboardSummaryQuery query, CancellationToken cancellationToken);
}
