using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FinanceHub.AuthConsent.Domain.Entities;
using FinanceHub.AuthConsent.Infrastructure.Persistence;
using FinanceHub.AuthConsent.Infrastructure.Persistence.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;
using Xunit;

namespace FinanceHub.UnitTests.AuthConsent.Infrastructure;

public class BankConsentRepositoryPersistenceTests
{
    private readonly FakeTimeProvider _timeProvider;

    public BankConsentRepositoryPersistenceTests()
    {
        _timeProvider = new FakeTimeProvider(new DateTimeOffset(2026, 8, 12, 12, 0, 0, TimeSpan.Zero));
    }

    private static AuthConsentDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<AuthConsentDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new AuthConsentDbContext(options);
    }

    [Fact]
    public async Task AddAsync_WithPendingConsent_ShouldPersistCorrectly()
    {
        // Arrange
        using var dbContext = CreateInMemoryDbContext();
        var repository = new BankConsentRepository(dbContext);

        var consent = BankConsent.Request("user-100", "itau", "ext-consent-100", _timeProvider);

        // Act
        await repository.AddAsync(consent, CancellationToken.None);
        await dbContext.SaveChangesAsync();

        // Assert
        var savedConsent = await dbContext.BankConsents.FirstOrDefaultAsync(c => c.Id == consent.Id);
        savedConsent.Should().NotBeNull();
        savedConsent!.UserId.Should().Be("user-100");
        savedConsent.InstitutionId.Should().Be("itau");
        savedConsent.Status.Should().Be(ConsentStatus.Pending);
        savedConsent.Token.ExternalConsentId.Should().Be("ext-consent-100");
        savedConsent.Token.AccessToken.Should().BeNull();
    }

    [Fact]
    public async Task UpdateAsync_WithAuthorizedConsent_ShouldPersistOwnedEntityTokenData()
    {
        // Arrange
        using var dbContext = CreateInMemoryDbContext();
        var repository = new BankConsentRepository(dbContext);

        var consent = BankConsent.Request("user-200", "mercadopago", "ext-consent-200", _timeProvider);
        await repository.AddAsync(consent, CancellationToken.None);
        await dbContext.SaveChangesAsync();

        // Act
        consent.Authorize("access-token-999", "refresh-token-888", 3600, _timeProvider);
        await repository.UpdateAsync(consent, CancellationToken.None);
        await dbContext.SaveChangesAsync();

        // Assert
        var updatedConsent = await dbContext.BankConsents.FirstOrDefaultAsync(c => c.Id == consent.Id);
        updatedConsent.Should().NotBeNull();
        updatedConsent!.Status.Should().Be(ConsentStatus.Authorized);
        updatedConsent.Token.AccessToken.Should().Be("access-token-999");
        updatedConsent.Token.RefreshToken.Should().Be("refresh-token-888");
        updatedConsent.Token.ExpiresAtUtc.Should().Be(_timeProvider.GetUtcNow().UtcDateTime.AddSeconds(3600));
    }

    [Fact]
    public async Task GetByUserIdAsync_ShouldReturnAllConsentsForUser()
    {
        // Arrange
        using var dbContext = CreateInMemoryDbContext();
        var repository = new BankConsentRepository(dbContext);

        var consent1 = BankConsent.Request("user-300", "itau", "ext-1", _timeProvider);
        var consent2 = BankConsent.Request("user-300", "inter", "ext-2", _timeProvider);

        await repository.AddAsync(consent1, CancellationToken.None);
        await repository.AddAsync(consent2, CancellationToken.None);
        await dbContext.SaveChangesAsync();

        // Act
        var result = await repository.GetByUserIdAsync("user-300", CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result.Select(c => c.InstitutionId).Should().Contain(new[] { "itau", "inter" });
    }

    [Fact]
    public async Task GetExpiringConsentsAsync_ShouldReturnConsentsExpiringWithinThreshold()
    {
        // Arrange
        using var dbContext = CreateInMemoryDbContext();
        var repository = new BankConsentRepository(dbContext);

        // Expiring in 3 minutes
        var consentExpiringSoon = BankConsent.Request("user-1", "itau", "ext-1", _timeProvider);
        consentExpiringSoon.Authorize("acc-1", "ref-1", 180, _timeProvider);

        // Expiring in 2 hours
        var consentNotExpiringSoon = BankConsent.Request("user-2", "itau", "ext-2", _timeProvider);
        consentNotExpiringSoon.Authorize("acc-2", "ref-2", 7200, _timeProvider);

        await repository.AddAsync(consentExpiringSoon, CancellationToken.None);
        await repository.AddAsync(consentNotExpiringSoon, CancellationToken.None);
        await dbContext.SaveChangesAsync();

        // Act
        var expiringConsents = await repository.GetExpiringConsentsAsync(TimeSpan.FromMinutes(5), CancellationToken.None);

        // Assert
        expiringConsents.Should().NotBeNull();
        expiringConsents.Should().ContainSingle(c => c.Id == consentExpiringSoon.Id);
    }
}
