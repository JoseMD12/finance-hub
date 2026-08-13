using System;
using FinanceHub.AuthConsent.Domain.Exceptions;
using FinanceHub.AuthConsent.Domain.ValueObjects;
using FluentAssertions;
using Microsoft.Extensions.Time.Testing;
using Xunit;

namespace FinanceHub.UnitTests.AuthConsent.Domain;

public class ConsentTokenTests
{
    private readonly FakeTimeProvider _timeProvider;
    private readonly DateTimeOffset _now;

    public ConsentTokenTests()
    {
        _now = new DateTimeOffset(2026, 8, 12, 12, 0, 0, TimeSpan.Zero);
        _timeProvider = new FakeTimeProvider(_now);
    }

    [Fact]
    public void CreatePending_WithValidConsentId_ShouldSetPendingStateAndNullTokens()
    {
        // Act
        var token = ConsentToken.CreatePending("ext-consent-123");

        // Assert
        token.ExternalConsentId.Should().Be("ext-consent-123");
        token.AccessToken.Should().BeNull();
        token.RefreshToken.Should().BeNull();
        token.ExpiresAtUtc.Should().BeNull();
        token.TokenType.Should().Be("Bearer");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void CreatePending_WithNullOrEmptyConsentId_ShouldThrowException(string? invalidConsentId)
    {
        // Act
        Action act = () => ConsentToken.CreatePending(invalidConsentId!);

        // Assert
        act.Should().Throw<NullOrEmptyExternalConsentIdDomainException>()
            .WithMessage("*ExternalConsentId*nulo ou vazio*");
    }

    [Fact]
    public void CreateAuthorized_WithValidParameters_ShouldSetTokensAndCalculateExpiresAtUtc()
    {
        // Act
        var token = ConsentToken.CreateAuthorized(
            "ext-consent-123",
            "access-token-123",
            "refresh-token-123",
            3600,
            _timeProvider);

        // Assert
        token.ExternalConsentId.Should().Be("ext-consent-123");
        token.AccessToken.Should().Be("access-token-123");
        token.RefreshToken.Should().Be("refresh-token-123");
        token.ExpiresAtUtc.Should().Be(_now.UtcDateTime.AddSeconds(3600));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void CreateAuthorized_WithNullOrEmptyAccessToken_ShouldThrowException(string? invalidAccessToken)
    {
        // Act
        Action act = () => ConsentToken.CreateAuthorized(
            "ext-consent-123",
            invalidAccessToken!,
            "refresh-token-123",
            3600,
            _timeProvider);

        // Assert
        act.Should().Throw<NullOrEmptyAccessTokenDomainException>()
            .WithMessage("*AccessToken*vazio*");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void CreateAuthorized_WithNullOrEmptyRefreshToken_ShouldThrowException(string? invalidRefreshToken)
    {
        // Act
        Action act = () => ConsentToken.CreateAuthorized(
            "ext-consent-123",
            "access-token-123",
            invalidRefreshToken!,
            3600,
            _timeProvider);

        // Assert
        act.Should().Throw<NullOrEmptyRefreshTokenDomainException>()
            .WithMessage("*RefreshToken*vazio*");
    }

    [Fact]
    public void Rotate_WithValidParameters_ShouldReturnNewTokenWithUpdatedValuesAndSameConsentId()
    {
        // Arrange
        var initialToken = ConsentToken.CreateAuthorized(
            "ext-consent-123",
            "old-access-token",
            "old-refresh-token",
            3600,
            _timeProvider);

        _timeProvider.Advance(TimeSpan.FromMinutes(30));

        // Act
        var rotatedToken = initialToken.Rotate(
            "new-access-token",
            "new-refresh-token",
            7200,
            _timeProvider);

        // Assert
        rotatedToken.ExternalConsentId.Should().Be("ext-consent-123");
        rotatedToken.AccessToken.Should().Be("new-access-token");
        rotatedToken.RefreshToken.Should().Be("new-refresh-token");
        rotatedToken.ExpiresAtUtc.Should().Be(_now.AddMinutes(30).UtcDateTime.AddSeconds(7200));
    }

    [Fact]
    public void ConsentToken_ValueEquality_ShouldMatchIdenticalTokens()
    {
        // Arrange
        var token1 = ConsentToken.CreatePending("ext-consent-123");
        var token2 = ConsentToken.CreatePending("ext-consent-123");

        // Assert
        token1.Should().Be(token2);
    }
}
