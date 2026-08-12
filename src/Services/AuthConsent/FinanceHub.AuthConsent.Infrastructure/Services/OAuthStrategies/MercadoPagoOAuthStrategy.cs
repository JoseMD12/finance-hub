using FinanceHub.AuthConsent.Application.DTOs;
using FinanceHub.AuthConsent.Application.Interfaces;
using FinanceHub.AuthConsent.Domain.Constants;

namespace FinanceHub.AuthConsent.Infrastructure.Services.OAuthStrategies;

public sealed class MercadoPagoOAuthStrategy : IOAuthBankClientStrategy
{
    public string InstitutionId => BankIdentifiers.MercadoPago;

    public Task<string> RequestConsentIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        var mockExternalConsentId = TokenMockGenerator.Generate(BankPrefixes.MercadoPago, TokenActions.Consent);
        return Task.FromResult(mockExternalConsentId);
    }

    public Task<OAuthTokenExchangeResult> ExchangeCodeForTokensAsync(string authCode, string redirectUri, CancellationToken cancellationToken = default)
    {
        var result = new OAuthTokenExchangeResult(
            AccessToken: TokenMockGenerator.Generate(BankPrefixes.MercadoPago, TokenActions.Access),
            RefreshToken: TokenMockGenerator.Generate(BankPrefixes.MercadoPago, TokenActions.Refresh),
            ExpiresInSeconds: 3600
        );

        return Task.FromResult(result);
    }

    public Task<OAuthTokenExchangeResult> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        var result = new OAuthTokenExchangeResult(
            AccessToken: TokenMockGenerator.Generate(BankPrefixes.MercadoPago, TokenActions.Access, TokenActions.Renewed),
            RefreshToken: TokenMockGenerator.Generate(BankPrefixes.MercadoPago, TokenActions.Refresh, TokenActions.Renewed),
            ExpiresInSeconds: 3600
        );

        return Task.FromResult(result);
    }
}
