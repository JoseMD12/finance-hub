using System.Threading;
using System.Threading.Tasks;
using FinanceHub.TransactionAggregator.Application.DTOs;
using FinanceHub.TransactionAggregator.Application.Interfaces;

namespace FinanceHub.TransactionAggregator.Application.Queries.GetTransactions;

public class GetTransactionsQueryHandler : IGetTransactionsQueryHandler
{
    private readonly ITransactionRepository _repository;

    public GetTransactionsQueryHandler(ITransactionRepository repository)
    {
        _repository = repository;
    }

    public async Task<PagedTransactionsResponseDto> Handle(GetTransactionsQuery query, CancellationToken cancellationToken)
    {
        return await _repository.QueryPagedByFilterAsync(query.Filter, cancellationToken);
    }
}
