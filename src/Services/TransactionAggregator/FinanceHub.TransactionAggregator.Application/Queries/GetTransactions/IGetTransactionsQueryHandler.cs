using System.Threading;
using System.Threading.Tasks;
using FinanceHub.TransactionAggregator.Application.DTOs;

namespace FinanceHub.TransactionAggregator.Application.Queries.GetTransactions;

public interface IGetTransactionsQueryHandler
{
    Task<PagedTransactionsResponseDto> Handle(GetTransactionsQuery query, CancellationToken cancellationToken);
}
