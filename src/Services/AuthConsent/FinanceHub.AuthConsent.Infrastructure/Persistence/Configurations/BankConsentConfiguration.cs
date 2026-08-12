using FinanceHub.AuthConsent.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinanceHub.AuthConsent.Infrastructure.Persistence.Configurations;

public class BankConsentConfiguration : IEntityTypeConfiguration<BankConsent>
{
    public void Configure(EntityTypeBuilder<BankConsent> builder)
    {
        builder.ToTable("bank_consents");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).HasColumnName("id");

        builder.Property(c => c.UserId)
            .HasColumnName("user_id")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(c => c.InstitutionId)
            .HasColumnName("institution_id")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(c => c.Status)
            .HasColumnName("status")
            .IsRequired();

        builder.Property(c => c.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();

        builder.Property(c => c.UpdatedAtUtc)
            .HasColumnName("updated_at_utc")
            .IsRequired();

        builder.OwnsOne(c => c.Token, tokenBuilder =>
        {
            tokenBuilder.Property(t => t.ExternalConsentId)
                .HasColumnName("consent_token")
                .IsRequired();

            tokenBuilder.Property(t => t.AccessToken)
                .HasColumnName("access_token");

            tokenBuilder.Property(t => t.RefreshToken)
                .HasColumnName("refresh_token");

            tokenBuilder.Property(t => t.TokenType)
                .HasColumnName("token_type")
                .HasMaxLength(20)
                .HasDefaultValue("Bearer");

            tokenBuilder.Property(t => t.ExpiresAtUtc)
                .HasColumnName("expires_at_utc");
        });

        builder.HasIndex(c => c.UserId).HasDatabaseName("idx_bank_consents_user_id");
        builder.HasIndex(c => new { c.Status, c.Token.ExpiresAtUtc }).HasDatabaseName("idx_bank_consents_status_expires");
    }
}
