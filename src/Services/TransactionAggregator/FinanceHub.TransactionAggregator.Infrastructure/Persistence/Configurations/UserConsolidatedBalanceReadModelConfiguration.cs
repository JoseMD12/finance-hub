using FinanceHub.TransactionAggregator.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinanceHub.TransactionAggregator.Infrastructure.Persistence.Configurations;

public class UserConsolidatedBalanceReadModelConfiguration : IEntityTypeConfiguration<UserConsolidatedBalanceReadModel>
{
    public void Configure(EntityTypeBuilder<UserConsolidatedBalanceReadModel> builder)
    {
        builder.ToTable("user_consolidated_balance_read_model");

        builder.HasKey(r => r.UserId);

        builder.Property(r => r.UserId)
            .HasColumnName("user_id")
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(r => r.TotalCheckingBalance)
            .HasColumnName("total_checking_balance")
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(r => r.TotalCreditCardSpent)
            .HasColumnName("total_credit_card_spent")
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(r => r.NetConsolidatedBalance)
            .HasColumnName("net_consolidated_balance")
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(r => r.LastCalculatedAtUtc)
            .HasColumnName("last_calculated_at_utc")
            .IsRequired();
    }
}
