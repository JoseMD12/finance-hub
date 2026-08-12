using FinanceHub.AuthConsent.Application.DTOs;

namespace FinanceHub.AuthConsent.Application.Interfaces;

public interface IOAuthBankClientStrategy
{
    string InstitutionId { get; }
    Task<string> RequestConsentIdAsync(string userId, CancellationToken cancellationToken = default);
    Task<OAuthTokenExchangeResult> ExchangeCodeForTokensAsync(string authCode, string redirectUri, CancellationToken cancellationToken = default);
    Task<OAuthTokenExchangeResult> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default);
}
