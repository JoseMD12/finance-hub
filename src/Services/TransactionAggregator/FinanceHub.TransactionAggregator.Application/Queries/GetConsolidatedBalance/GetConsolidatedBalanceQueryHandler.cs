using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FinanceHub.TransactionAggregator.Application.DTOs;
using FinanceHub.TransactionAggregator.Application.Interfaces;

namespace FinanceHub.TransactionAggregator.Application.Queries.GetConsolidatedBalance;

public record GetConsolidatedBalanceQuery(string UserId);

public class GetConsolidatedBalanceQueryHandler : IGetConsolidatedBalanceQueryHandler
{
    private readonly IAccountBalanceRepository _repository;

    public GetConsolidatedBalanceQueryHandler(IAccountBalanceRepository repository)
    {
        _repository = repository;
    }

    public async Task<ConsolidatedBalanceDto> Handle(GetConsolidatedBalanceQuery query, CancellationToken cancellationToken)
    {
        var balanceDtos = (await _repository.GetProjectedByUserIdAsync(query.UserId, cancellationToken)).ToList();

        var totalBrl = balanceDtos
            .Where(b => b.Currency == "BRL")
            .Sum(b => b.Amount);

        return new ConsolidatedBalanceDto(query.UserId, totalBrl, balanceDtos);
    }
}
