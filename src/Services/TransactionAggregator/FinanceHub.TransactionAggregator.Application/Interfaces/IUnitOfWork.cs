using System.Threading;
using System.Threading.Tasks;

namespace FinanceHub.TransactionAggregator.Application.Interfaces;

public interface IUnitOfWork
{
    Task<int> CommitAsync(CancellationToken cancellationToken = default);
}
