using FinanceHub.MercadoPagoIntegration.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinanceHub.MercadoPagoIntegration.Infrastructure.Persistence.Configurations;

public class MercadoPagoSyncStateConfiguration : IEntityTypeConfiguration<MercadoPagoSyncState>
{
    public void Configure(EntityTypeBuilder<MercadoPagoSyncState> builder)
    {
        builder.ToTable("MercadoPagoSyncStates");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.UserId)
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(x => x.AccountId)
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(x => x.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(32);

        builder.Property(x => x.LastErrorMessage)
            .HasMaxLength(2048);

        builder.Property(x => x.LastSyncCursorUtc)
            .IsRequired();

        builder.Property(x => x.LastExecutionUtc)
            .IsRequired();

        builder.Property(x => x.CreatedAtUtc)
            .IsRequired();

        builder.Property(x => x.UpdatedAtUtc)
            .IsRequired();

        builder.HasIndex(x => new { x.UserId, x.AccountId })
            .IsUnique();
    }
}
