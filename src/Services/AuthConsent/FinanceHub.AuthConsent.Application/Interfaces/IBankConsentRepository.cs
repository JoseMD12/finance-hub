using FinanceHub.AuthConsent.Domain.Entities;

namespace FinanceHub.AuthConsent.Application.Interfaces;

public interface IBankConsentRepository
{
    Task AddAsync(BankConsent consent, CancellationToken cancellationToken = default);
    Task<BankConsent?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<BankConsent>> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default);
    Task<IEnumerable<BankConsent>> GetExpiringConsentsAsync(TimeSpan threshold, CancellationToken cancellationToken = default);
    Task UpdateAsync(BankConsent consent, CancellationToken cancellationToken = default);
}
