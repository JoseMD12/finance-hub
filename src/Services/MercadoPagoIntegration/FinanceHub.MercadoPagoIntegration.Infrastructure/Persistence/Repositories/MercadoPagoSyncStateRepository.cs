using FinanceHub.MercadoPagoIntegration.Application.Interfaces;
using FinanceHub.MercadoPagoIntegration.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FinanceHub.MercadoPagoIntegration.Infrastructure.Persistence.Repositories;

public class MercadoPagoSyncStateRepository : IMercadoPagoSyncStateRepository
{
    private readonly MercadoPagoDbContext _dbContext;

    public MercadoPagoSyncStateRepository(MercadoPagoDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<MercadoPagoSyncState?> GetByAccountAsync(string userId, string accountId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.SyncStates
            .FirstOrDefaultAsync(x => x.UserId == userId && x.AccountId == accountId, cancellationToken);
    }

    public async Task<MercadoPagoSyncState?> GetLatestByUserAsync(string userId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.SyncStates
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.LastExecutionUtc)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task AddAsync(MercadoPagoSyncState syncState, CancellationToken cancellationToken = default)
    {
        await _dbContext.SyncStates.AddAsync(syncState, cancellationToken);
    }

    public Task UpdateAsync(MercadoPagoSyncState syncState, CancellationToken cancellationToken = default)
    {
        _dbContext.SyncStates.Update(syncState);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
