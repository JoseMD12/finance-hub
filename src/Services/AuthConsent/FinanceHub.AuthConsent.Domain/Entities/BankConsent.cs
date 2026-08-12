using FinanceHub.AuthConsent.Domain.Events;
using FinanceHub.AuthConsent.Domain.Exceptions;
using FinanceHub.AuthConsent.Domain.ValueObjects;

namespace FinanceHub.AuthConsent.Domain.Entities;

public class BankConsent
{
    private readonly List<object> _domainEvents = [];

    public Guid Id { get; private set; }
    public string UserId { get; private set; }
    public string InstitutionId { get; private set; }
    public ConsentToken Token { get; private set; }
    public ConsentStatus Status { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }

    public IReadOnlyCollection<object> DomainEvents => _domainEvents.AsReadOnly();

    private BankConsent()
    {
        UserId = null!;
        InstitutionId = null!;
        Token = null!;
    }

    public static BankConsent Request(
        string userId,
        string institutionId,
        string externalConsentId,
        TimeProvider timeProvider)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new NullOrEmptyUserIdDomainException();

        if (string.IsNullOrWhiteSpace(institutionId))
            throw new NullOrEmptyInstitutionIdDomainException();

        var now = timeProvider.GetUtcNow().UtcDateTime;

        return new BankConsent
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            InstitutionId = institutionId.ToLowerInvariant(),
            Token = ConsentToken.CreatePending(externalConsentId),
            Status = ConsentStatus.Pending,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };
    }

    public void Authorize(
        string accessToken,
        string refreshToken,
        int expiresInSeconds,
        TimeProvider timeProvider)
    {
        if (Status != ConsentStatus.Pending)
            throw new ConsentInvalidStateException(Status.ToString(), "Authorize");

        var now = timeProvider.GetUtcNow().UtcDateTime;
        Token = ConsentToken.CreateAuthorized(Token.ExternalConsentId, accessToken, refreshToken, expiresInSeconds, timeProvider);
        Status = ConsentStatus.Authorized;
        UpdatedAtUtc = now;

        _domainEvents.Add(new ConsentAuthorizedDomainEvent(Id, UserId, InstitutionId, now));
    }

    public void RotateTokens(
        string newAccessToken,
        string newRefreshToken,
        int expiresInSeconds,
        TimeProvider timeProvider)
    {
        if (Status != ConsentStatus.Authorized)
            throw new ConsentInvalidStateException(Status.ToString(), "RotateTokens");

        var now = timeProvider.GetUtcNow().UtcDateTime;
        Token = Token.Rotate(newAccessToken, newRefreshToken, expiresInSeconds, timeProvider);
        UpdatedAtUtc = now;
    }

    public void Revoke(TimeProvider timeProvider)
    {
        if (Status == ConsentStatus.Revoked)
            return;

        Status = ConsentStatus.Revoked;
        UpdatedAtUtc = timeProvider.GetUtcNow().UtcDateTime;
    }

    public bool IsExpiringSoon(TimeProvider timeProvider, int thresholdMinutes = 5)
    {
        if (Status != ConsentStatus.Authorized || Token.ExpiresAtUtc is null)
            return false;

        var now = timeProvider.GetUtcNow().UtcDateTime;
        return Token.ExpiresAtUtc.Value <= now.AddMinutes(thresholdMinutes);
    }

    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
}
