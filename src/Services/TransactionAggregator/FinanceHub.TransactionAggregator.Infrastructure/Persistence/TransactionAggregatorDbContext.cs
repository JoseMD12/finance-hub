using FinanceHub.TransactionAggregator.Domain.Entities;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace FinanceHub.TransactionAggregator.Infrastructure.Persistence;

public class TransactionAggregatorDbContext : DbContext
{
    public DbSet<CanonicalTransaction> Transactions => Set<CanonicalTransaction>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<AccountBalance> AccountBalances => Set<AccountBalance>();
    public DbSet<UserCategoryRule> UserCategoryRules => Set<UserCategoryRule>();
    public DbSet<InboxProcessedMessage> InboxProcessedMessages => Set<InboxProcessedMessage>();
    public DbSet<UserConsolidatedBalanceReadModel> UserConsolidatedBalances => Set<UserConsolidatedBalanceReadModel>();

    public TransactionAggregatorDbContext(DbContextOptions<TransactionAggregatorDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.AddTransactionalOutboxEntities();
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TransactionAggregatorDbContext).Assembly);
    }
}
