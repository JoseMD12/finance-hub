using FinanceHub.MercadoPagoIntegration.Domain.Entities;

namespace FinanceHub.MercadoPagoIntegration.Application.Interfaces;

public interface IMercadoPagoSyncStateRepository
{
    Task<MercadoPagoSyncState?> GetByAccountAsync(string userId, string accountId, CancellationToken cancellationToken = default);
    Task<MercadoPagoSyncState?> GetLatestByUserAsync(string userId, CancellationToken cancellationToken = default);
    Task AddAsync(MercadoPagoSyncState syncState, CancellationToken cancellationToken = default);
    Task UpdateAsync(MercadoPagoSyncState syncState, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
