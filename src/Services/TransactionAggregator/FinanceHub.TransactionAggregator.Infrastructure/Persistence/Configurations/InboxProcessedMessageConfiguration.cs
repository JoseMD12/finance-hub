using FinanceHub.TransactionAggregator.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinanceHub.TransactionAggregator.Infrastructure.Persistence.Configurations;

public class InboxProcessedMessageConfiguration : IEntityTypeConfiguration<InboxProcessedMessage>
{
    public void Configure(EntityTypeBuilder<InboxProcessedMessage> builder)
    {
        builder.ToTable("inbox_processed_messages");

        builder.HasKey(m => m.MessageHash);

        builder.Property(m => m.MessageHash)
            .HasColumnName("message_hash")
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(m => m.EventType)
            .HasColumnName("event_type")
            .HasMaxLength(250)
            .IsRequired();

        builder.Property(m => m.ProcessedAtUtc)
            .HasColumnName("processed_at_utc")
            .IsRequired();
    }
}
