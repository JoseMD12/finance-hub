using FinanceHub.TransactionAggregator.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinanceHub.TransactionAggregator.Infrastructure.Persistence.Configurations;

public class AccountBalanceConfiguration : IEntityTypeConfiguration<AccountBalance>
{
    public void Configure(EntityTypeBuilder<AccountBalance> builder)
    {
        builder.ToTable("account_balances");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.UserId)
            .IsRequired()
            .HasMaxLength(128);

        builder.OwnsOne(x => x.AccountInfo, acc =>
        {
            acc.Property(a => a.InstitutionId)
                .HasColumnName("institution_id")
                .IsRequired()
                .HasMaxLength(64);

            acc.Property(a => a.AccountId)
                .HasColumnName("account_id")
                .IsRequired()
                .HasMaxLength(128);
        });

        builder.OwnsOne(x => x.CurrentBalance, money =>
        {
            money.Property(m => m.Amount)
                .HasColumnName("current_balance")
                .HasPrecision(18, 2)
                .IsRequired();

            money.Property(m => m.Currency)
                .HasColumnName("currency")
                .HasMaxLength(3)
                .IsRequired();
        });

        builder.Property(x => x.LastUpdatedAtUtc)
            .IsRequired();

        // Optimistic Concurrency Token via xmin system column in PostgreSQL
        builder.Property<uint>("xmin")
            .HasColumnName("xmin")
            .HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate()
            .IsRowVersion();
    }
}
