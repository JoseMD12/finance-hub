using System.Threading;
using System.Threading.Tasks;

namespace FinanceHub.TransactionAggregator.Application.Interfaces;

public interface ITransferPairMatchingEngine
{
    Task<int> MatchAndPairAsync(string userId, CancellationToken cancellationToken = default);
}
