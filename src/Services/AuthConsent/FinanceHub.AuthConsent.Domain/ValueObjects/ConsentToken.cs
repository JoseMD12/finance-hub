using FinanceHub.AuthConsent.Domain.Exceptions;

namespace FinanceHub.AuthConsent.Domain.ValueObjects;

public sealed record ConsentToken
{
    public string ExternalConsentId { get; private set; }
    public string? AccessToken { get; private set; }
    public string? RefreshToken { get; private set; }
    public DateTime? ExpiresAtUtc { get; private set; }
    public string TokenType { get; private set; }

    private ConsentToken(
        string externalConsentId,
        string? accessToken,
        string? refreshToken,
        DateTime? expiresAtUtc,
        string tokenType = "Bearer")
    {
        if (string.IsNullOrWhiteSpace(externalConsentId))
            throw new ConsentDomainException("ExternalConsentId não pode ser nulo ou vazio.");

        ExternalConsentId = externalConsentId;
        AccessToken = accessToken;
        RefreshToken = refreshToken;
        ExpiresAtUtc = expiresAtUtc;
        TokenType = tokenType;
    }

    public static ConsentToken CreatePending(string externalConsentId)
    {
        return new ConsentToken(externalConsentId, null, null, null);
    }

    public static ConsentToken CreateAuthorized(
        string externalConsentId,
        string accessToken,
        string refreshToken,
        int expiresInSeconds,
        TimeProvider timeProvider)
    {
        if (string.IsNullOrWhiteSpace(accessToken))
            throw new ConsentDomainException("AccessToken não pode ser vazio para autorização.");

        if (string.IsNullOrWhiteSpace(refreshToken))
            throw new ConsentDomainException("RefreshToken não pode ser vazio para autorização.");

        var expiresAtUtc = timeProvider.GetUtcNow().UtcDateTime.AddSeconds(expiresInSeconds);
        return new ConsentToken(externalConsentId, accessToken, refreshToken, expiresAtUtc);
    }

    public ConsentToken Rotate(
        string newAccessToken,
        string newRefreshToken,
        int expiresInSeconds,
        TimeProvider timeProvider)
    {
        return CreateAuthorized(
            ExternalConsentId,
            newAccessToken,
            newRefreshToken,
            expiresInSeconds,
            timeProvider
        );
    }
}
