using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FinanceHub.TransactionAggregator.Application.DTOs;
using FinanceHub.TransactionAggregator.Application.Interfaces;

namespace FinanceHub.TransactionAggregator.Application.Queries.GetTransactions;

public record GetTransactionsQuery(string UserId, int Page = 1, int PageSize = 20);

public interface IGetTransactionsQueryHandler
{
    Task<IEnumerable<TransactionDto>> Handle(GetTransactionsQuery query, CancellationToken cancellationToken);
}

public class GetTransactionsQueryHandler : IGetTransactionsQueryHandler
{
    private readonly ITransactionRepository _repository;

    public GetTransactionsQueryHandler(ITransactionRepository repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<TransactionDto>> Handle(GetTransactionsQuery query, CancellationToken cancellationToken)
    {
        var transactions = await _repository.GetByUserIdAsync(query.UserId, query.Page, query.PageSize, cancellationToken);

        return transactions.Select(t => new TransactionDto(
            t.Id,
            t.UserId,
            t.AccountInfo.InstitutionId,
            t.AccountInfo.AccountId,
            t.Amount.Amount,
            t.Amount.Currency,
            t.Type.ToString(),
            t.Description.CleanText,
            t.CategoryId,
            t.CategorizationSource.ToString(),
            t.IsManuallyCategorized,
            t.TransactionDateUtc,
            t.BankDetails.Channel.ToString(),
            t.BankDetails.MerchantName));
    }
}
