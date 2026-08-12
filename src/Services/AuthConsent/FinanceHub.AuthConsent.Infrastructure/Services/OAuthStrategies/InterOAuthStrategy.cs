using FinanceHub.AuthConsent.Application.DTOs;
using FinanceHub.AuthConsent.Application.Interfaces;
using FinanceHub.AuthConsent.Domain.Constants;

namespace FinanceHub.AuthConsent.Infrastructure.Services.OAuthStrategies;

public sealed class InterOAuthStrategy : IOAuthBankClientStrategy
{
    public string InstitutionId => BankIdentifiers.Inter;

    public Task<string> RequestConsentIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        var mockExternalConsentId = TokenMockGenerator.Generate(BankPrefixes.Inter, TokenActions.Consent);
        return Task.FromResult(mockExternalConsentId);
    }

    public Task<OAuthTokenExchangeResult> ExchangeCodeForTokensAsync(string authCode, string redirectUri, CancellationToken cancellationToken = default)
    {
        var result = new OAuthTokenExchangeResult(
            AccessToken: TokenMockGenerator.Generate(BankPrefixes.Inter, TokenActions.Access),
            RefreshToken: TokenMockGenerator.Generate(BankPrefixes.Inter, TokenActions.Refresh),
            ExpiresInSeconds: 3600
        );

        return Task.FromResult(result);
    }

    public Task<OAuthTokenExchangeResult> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        var result = new OAuthTokenExchangeResult(
            AccessToken: TokenMockGenerator.Generate(BankPrefixes.Inter, TokenActions.Access, TokenActions.Renewed),
            RefreshToken: TokenMockGenerator.Generate(BankPrefixes.Inter, TokenActions.Refresh, TokenActions.Renewed),
            ExpiresInSeconds: 3600
        );

        return Task.FromResult(result);
    }
}
