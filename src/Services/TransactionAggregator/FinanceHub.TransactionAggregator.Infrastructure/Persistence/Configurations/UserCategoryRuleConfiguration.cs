using FinanceHub.TransactionAggregator.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinanceHub.TransactionAggregator.Infrastructure.Persistence.Configurations;

public class UserCategoryRuleConfiguration : IEntityTypeConfiguration<UserCategoryRule>
{
    public void Configure(EntityTypeBuilder<UserCategoryRule> builder)
    {
        builder.ToTable("user_category_rules");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.UserId)
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(x => x.Pattern)
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(x => x.CategoryId)
            .IsRequired();

        builder.Property(x => x.CreatedAtUtc)
            .IsRequired();

        builder.HasIndex(x => new { x.UserId, x.Pattern })
            .IsUnique()
            .HasDatabaseName("idx_user_category_rules_user_pattern");

        // Optimistic Concurrency Token via xmin system column in PostgreSQL
        builder.Property<uint>("xmin")
            .HasColumnName("xmin")
            .HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate()
            .IsRowVersion();
    }
}
