using System.Threading;
using System.Threading.Tasks;
using FinanceHub.TransactionAggregator.Application.Interfaces;

namespace FinanceHub.TransactionAggregator.Infrastructure.Persistence;

public class UnitOfWork : IUnitOfWork
{
    private readonly TransactionAggregatorDbContext _dbContext;

    public UnitOfWork(TransactionAggregatorDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<int> CommitAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
