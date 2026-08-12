using FinanceHub.AuthConsent.Domain.Entities;
using FinanceHub.AuthConsent.Domain.Exceptions;
using FinanceHub.AuthConsent.Domain.ValueObjects;
using FluentAssertions;
using Microsoft.Extensions.Time.Testing;
using Xunit;

namespace FinanceHub.UnitTests.AuthConsent.Domain;

public class BankConsentTests
{
    private readonly FakeTimeProvider _fakeTimeProvider = new();

    public BankConsentTests()
    {
        _fakeTimeProvider.SetUtcNow(new DateTimeOffset(2026, 8, 11, 12, 0, 0, TimeSpan.Zero));
    }

    [Fact]
    public void RequestConsent_ComDadosValidos_DeveCriarConsentimentoEmStatusPending()
    {
        var consent = BankConsent.Request(
            userId: "user-123",
            institutionId: "itau",
            externalConsentId: "consent-xyz-999",
            timeProvider: _fakeTimeProvider
        );

        consent.Should().NotBeNull();
        consent.UserId.Should().Be("user-123");
        consent.InstitutionId.Should().Be("itau");
        consent.Status.Should().Be(ConsentStatus.Pending);
        consent.Token.ExternalConsentId.Should().Be("consent-xyz-999");
        consent.CreatedAtUtc.Should().Be(_fakeTimeProvider.GetUtcNow().UtcDateTime);
    }

    [Fact]
    public void RequestConsent_ComUserIdVazio_DeveLancarNullOrEmptyUserIdDomainException()
    {
        var act = () => BankConsent.Request(
            userId: "",
            institutionId: "itau",
            externalConsentId: "consent-xyz-999",
            timeProvider: _fakeTimeProvider
        );

        act.Should().Throw<NullOrEmptyUserIdDomainException>()
           .WithMessage("UserId não pode ser nulo ou vazio.");
    }

    [Fact]
    public void RequestConsent_ComInstitutionIdVazio_DeveLancarNullOrEmptyInstitutionIdDomainException()
    {
        var act = () => BankConsent.Request(
            userId: "user-123",
            institutionId: "",
            externalConsentId: "consent-xyz-999",
            timeProvider: _fakeTimeProvider
        );

        act.Should().Throw<NullOrEmptyInstitutionIdDomainException>()
           .WithMessage("InstitutionId não pode ser nulo ou vazio.");
    }

    [Fact]
    public void RequestConsent_ComExternalConsentIdVazio_DeveLancarNullOrEmptyExternalConsentIdDomainException()
    {
        var act = () => BankConsent.Request(
            userId: "user-123",
            institutionId: "itau",
            externalConsentId: "",
            timeProvider: _fakeTimeProvider
        );

        act.Should().Throw<NullOrEmptyExternalConsentIdDomainException>()
           .WithMessage("ExternalConsentId não pode ser nulo ou vazio.");
    }

    [Fact]
    public void Authorize_ComAccessTokenVazio_DeveLancarNullOrEmptyAccessTokenDomainException()
    {
        var consent = BankConsent.Request("user-123", "itau", "consent-999", _fakeTimeProvider);

        var act = () => consent.Authorize(
            accessToken: "",
            refreshToken: "ref-123",
            expiresInSeconds: 3600,
            timeProvider: _fakeTimeProvider
        );

        act.Should().Throw<NullOrEmptyAccessTokenDomainException>()
           .WithMessage("AccessToken não pode ser vazio para autorização.");
    }

    [Fact]
    public void Authorize_ComRefreshTokenVazio_DeveLancarNullOrEmptyRefreshTokenDomainException()
    {
        var consent = BankConsent.Request("user-123", "itau", "consent-999", _fakeTimeProvider);

        var act = () => consent.Authorize(
            accessToken: "acc-123",
            refreshToken: "",
            expiresInSeconds: 3600,
            timeProvider: _fakeTimeProvider
        );

        act.Should().Throw<NullOrEmptyRefreshTokenDomainException>()
           .WithMessage("RefreshToken não pode ser vazio para autorização.");
    }

    [Fact]
    public void Authorize_QuandoPendente_DeveAtualizarTokensEStatusParaAuthorized()
    {
        var consent = BankConsent.Request("user-123", "itau", "consent-999", _fakeTimeProvider);

        consent.Authorize(
            accessToken: "access-token-123",
            refreshToken: "refresh-token-456",
            expiresInSeconds: 3600,
            timeProvider: _fakeTimeProvider
        );

        consent.Status.Should().Be(ConsentStatus.Authorized);
        consent.Token.AccessToken.Should().Be("access-token-123");
        consent.Token.RefreshToken.Should().Be("refresh-token-456");
        consent.Token.ExpiresAtUtc.Should().Be(_fakeTimeProvider.GetUtcNow().UtcDateTime.AddSeconds(3600));
        consent.DomainEvents.Should().HaveCount(1);
    }

    [Fact]
    public void Authorize_QuandoJaRevogadoOuExpirado_DeveLancarConsentInvalidStateException()
    {
        var consent = BankConsent.Request("user-123", "itau", "consent-999", _fakeTimeProvider);
        consent.Revoke(_fakeTimeProvider);

        var act = () => consent.Authorize("acc-123", "ref-456", 3600, _fakeTimeProvider);

        act.Should().Throw<ConsentInvalidStateException>()
           .WithMessage("Consentimento no estado 'Revoked' não pode executar a ação 'Authorize'.");
    }

    [Fact]
    public void RotateTokens_QuandoTokensValidos_DeveSubstituirConsentTokenEDataExpiracao()
    {
        var consent = BankConsent.Request("user-123", "itau", "consent-999", _fakeTimeProvider);
        consent.Authorize("acc-old", "ref-old", 3600, _fakeTimeProvider);

        _fakeTimeProvider.Advance(TimeSpan.FromMinutes(30));

        consent.RotateTokens("acc-new-777", "ref-new-888", 3600, _fakeTimeProvider);

        consent.Token.AccessToken.Should().Be("acc-new-777");
        consent.Token.RefreshToken.Should().Be("ref-new-888");
        consent.Token.ExpiresAtUtc.Should().Be(_fakeTimeProvider.GetUtcNow().UtcDateTime.AddSeconds(3600));
        consent.UpdatedAtUtc.Should().Be(_fakeTimeProvider.GetUtcNow().UtcDateTime);
    }

    [Fact]
    public void Revoke_QuandoAtivo_DeveAlterarStatusParaRevoked()
    {
        var consent = BankConsent.Request("user-123", "itau", "consent-999", _fakeTimeProvider);
        consent.Authorize("acc-123", "ref-456", 3600, _fakeTimeProvider);

        consent.Revoke(_fakeTimeProvider);

        consent.Status.Should().Be(ConsentStatus.Revoked);
    }

    [Fact]
    public void IsExpiringSoon_QuandoFaltarMenosDe5Minutos_DeveRetornarTrue()
    {
        var consent = BankConsent.Request("user-123", "itau", "consent-999", _fakeTimeProvider);
        consent.Authorize("acc-123", "ref-456", 3600, _fakeTimeProvider);

        _fakeTimeProvider.Advance(TimeSpan.FromMinutes(56));

        consent.IsExpiringSoon(_fakeTimeProvider).Should().BeTrue();
    }
}
