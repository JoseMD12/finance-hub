using FinanceHub.TransactionAggregator.Domain.Entities;
using FinanceHub.TransactionAggregator.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinanceHub.TransactionAggregator.Infrastructure.Persistence.Configurations;

public class CanonicalTransactionConfiguration : IEntityTypeConfiguration<CanonicalTransaction>
{
    public void Configure(EntityTypeBuilder<CanonicalTransaction> builder)
    {
        builder.ToTable("canonical_transactions");

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

        builder.Property(x => x.Hash)
            .HasConversion(h => h.Value, v => new TransactionHash(v))
            .HasColumnName("transaction_hash")
            .IsRequired()
            .HasMaxLength(64);

        builder.HasIndex(x => x.Hash)
            .IsUnique()
            .HasDatabaseName("idx_canonical_transactions_hash");

        builder.OwnsOne(x => x.Amount, money =>
        {
            money.Property(m => m.Amount)
                .HasColumnName("amount")
                .HasPrecision(18, 2)
                .IsRequired();

            money.Property(m => m.Currency)
                .HasColumnName("currency")
                .HasMaxLength(3)
                .IsRequired();
        });

        builder.Property(x => x.Type)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.Description)
            .HasConversion(d => d.CleanText, v => SanitizedDescription.Create(v))
            .HasColumnName("description")
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(x => x.CategoryId)
            .IsRequired();

        builder.Property(x => x.CategorizationSource)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.IsManuallyCategorized)
            .IsRequired();

        builder.Property(x => x.TransactionDateUtc)
            .IsRequired();

        builder.OwnsOne(x => x.BankDetails, bd =>
        {
            bd.Property(b => b.BankTransactionId)
                .HasColumnName("bank_transaction_id")
                .HasMaxLength(128);

            bd.Property(b => b.Channel)
                .HasColumnName("channel")
                .HasConversion<int>();

            bd.Property(b => b.MerchantName)
                .HasColumnName("merchant_name")
                .HasMaxLength(128);
        });

        builder.OwnsOne(x => x.AuditInfo, ai =>
        {
            ai.Property(a => a.CreatedAtUtc)
                .HasColumnName("created_at_utc")
                .IsRequired();

            ai.Property(a => a.UpdatedAtUtc)
                .HasColumnName("updated_at_utc")
                .IsRequired();
        });
    }
}
