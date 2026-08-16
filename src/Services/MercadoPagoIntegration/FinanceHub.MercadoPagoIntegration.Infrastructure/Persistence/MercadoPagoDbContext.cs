using FinanceHub.MercadoPagoIntegration.Domain.Entities;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace FinanceHub.MercadoPagoIntegration.Infrastructure.Persistence;

public class MercadoPagoDbContext : DbContext
{
    public DbSet<MercadoPagoSyncState> SyncStates => Set<MercadoPagoSyncState>();

    public MercadoPagoDbContext(DbContextOptions<MercadoPagoDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(MercadoPagoDbContext).Assembly);

        // MassTransit Outbox Entities
        modelBuilder.AddInboxStateEntity();
        modelBuilder.AddOutboxMessageEntity();
        modelBuilder.AddOutboxStateEntity();
    }
}
