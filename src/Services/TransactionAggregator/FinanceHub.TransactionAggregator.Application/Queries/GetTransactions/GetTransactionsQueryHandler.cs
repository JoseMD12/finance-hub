using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FinanceHub.TransactionAggregator.Application.DTOs;
using FinanceHub.TransactionAggregator.Application.Interfaces;

namespace FinanceHub.TransactionAggregator.Application.Queries.GetTransactions;

public record GetTransactionsQuery(string UserId, int Page = 1, int PageSize = 20);

public class GetTransactionsQueryHandler : IGetTransactionsQueryHandler
{
    private readonly ITransactionRepository _repository;

    public GetTransactionsQueryHandler(ITransactionRepository repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<TransactionDto>> Handle(GetTransactionsQuery query, CancellationToken cancellationToken)
    {
        return await _repository.GetProjectedByUserIdAsync(query.UserId, query.Page, query.PageSize, cancellationToken);
    }
}
