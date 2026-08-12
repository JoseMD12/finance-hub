using FinanceHub.AuthConsent.Application.Interfaces;
using FinanceHub.AuthConsent.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FinanceHub.AuthConsent.Infrastructure.Persistence.Repositories;

public sealed class BankConsentRepository(AuthConsentDbContext dbContext) : IBankConsentRepository
{
    public async Task AddAsync(BankConsent consent, CancellationToken cancellationToken = default)
    {
        await dbContext.BankConsents.AddAsync(consent, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<BankConsent?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await dbContext.BankConsents.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<BankConsent>> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        return await dbContext.BankConsents
            .AsNoTracking()
            .Where(c => c.UserId == userId)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<BankConsent>> GetExpiringConsentsAsync(TimeSpan threshold, CancellationToken cancellationToken = default)
    {
        var cutoffTime = DateTime.UtcNow.Add(threshold);

        return await dbContext.BankConsents
            .Where(c => c.Status == ConsentStatus.Authorized &&
                        c.Token.ExpiresAtUtc != null &&
                        c.Token.ExpiresAtUtc <= cutoffTime)
            .ToListAsync(cancellationToken);
    }

    public async Task UpdateAsync(BankConsent consent, CancellationToken cancellationToken = default)
    {
        dbContext.BankConsents.Update(consent);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
