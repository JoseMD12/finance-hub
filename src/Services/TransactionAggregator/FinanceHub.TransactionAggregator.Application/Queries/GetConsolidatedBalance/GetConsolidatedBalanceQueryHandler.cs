using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FinanceHub.TransactionAggregator.Application.DTOs;
using FinanceHub.TransactionAggregator.Application.Interfaces;

namespace FinanceHub.TransactionAggregator.Application.Queries.GetConsolidatedBalance;

public record GetConsolidatedBalanceQuery(string UserId);

public interface IGetConsolidatedBalanceQueryHandler
{
    Task<ConsolidatedBalanceDto> Handle(GetConsolidatedBalanceQuery query, CancellationToken cancellationToken);
}

public class GetConsolidatedBalanceQueryHandler : IGetConsolidatedBalanceQueryHandler
{
    private readonly IAccountBalanceRepository _repository;

    public GetConsolidatedBalanceQueryHandler(IAccountBalanceRepository repository)
    {
        _repository = repository;
    }

    public async Task<ConsolidatedBalanceDto> Handle(GetConsolidatedBalanceQuery query, CancellationToken cancellationToken)
    {
        var balances = await _repository.GetByUserIdAsync(query.UserId, cancellationToken);
        var balanceDtos = balances.Select(b => new AccountBalanceDto(
            b.AccountInfo.InstitutionId,
            b.AccountInfo.AccountId,
            b.CurrentBalance.Amount,
            b.CurrentBalance.Currency,
            b.LastUpdatedAtUtc)).ToList();

        var totalBrl = balanceDtos
            .Where(b => b.Currency == "BRL")
            .Sum(b => b.Amount);

        return new ConsolidatedBalanceDto(query.UserId, totalBrl, balanceDtos);
    }
}
