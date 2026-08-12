using FinanceHub.AuthConsent.Domain.Entities;
using FinanceHub.AuthConsent.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;

namespace FinanceHub.AuthConsent.Infrastructure.Persistence;

public class AuthConsentDbContext(DbContextOptions<AuthConsentDbContext> options) : DbContext(options)
{
    public DbSet<BankConsent> BankConsents => Set<BankConsent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfiguration(new BankConsentConfiguration());
    }
}
