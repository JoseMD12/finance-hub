using FinanceHub.TransactionAggregator.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FinanceHub.TransactionAggregator.Infrastructure.Persistence;

public class TransactionAggregatorDbContext : DbContext
{
    public DbSet<CanonicalTransaction> Transactions => Set<CanonicalTransaction>();
    public DbSet<AccountBalance> AccountBalances => Set<AccountBalance>();
    public DbSet<UserCategoryRule> UserCategoryRules => Set<UserCategoryRule>();

    public TransactionAggregatorDbContext(DbContextOptions<TransactionAggregatorDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TransactionAggregatorDbContext).Assembly);
    }
}
