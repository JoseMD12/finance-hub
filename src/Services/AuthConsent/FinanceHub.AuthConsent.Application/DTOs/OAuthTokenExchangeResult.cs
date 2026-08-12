namespace FinanceHub.AuthConsent.Application.DTOs;

public record OAuthTokenExchangeResult(
    string AccessToken,
    string RefreshToken,
    int ExpiresInSeconds
);
