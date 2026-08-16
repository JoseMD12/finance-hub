namespace FinanceHub.AuthConsent.Application.DTOs;

public record InternalConsentTokenDto(
    string AccessToken,
    string? RefreshToken,
    int ExpiresInSeconds,
    bool IsAuthorised
);
