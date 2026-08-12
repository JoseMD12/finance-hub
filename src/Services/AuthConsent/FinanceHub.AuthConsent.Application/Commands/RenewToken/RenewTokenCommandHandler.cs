using FinanceHub.AuthConsent.Application.DTOs;
using FinanceHub.AuthConsent.Application.Exceptions;
using FinanceHub.AuthConsent.Application.Interfaces;

namespace FinanceHub.AuthConsent.Application.Commands.RenewToken;

public sealed class RenewTokenCommandHandler(
    IBankConsentRepository repository,
    IKeyedOAuthStrategyFactory strategyFactory,
    TimeProvider timeProvider) : IRenewTokenCommandHandler
{
    public async Task<OAuthTokenExchangeResult> Handle(RenewTokenCommand command, CancellationToken cancellationToken)
    {
        var consent = await repository.GetByIdAsync(command.ConsentId, cancellationToken)
                      ?? throw new ConsentNotFoundDomainException(command.ConsentId);

        if (string.IsNullOrWhiteSpace(consent.Token.RefreshToken))
        {
            throw new UnauthorizedBankDomainException(consent.InstitutionId);
        }

        var oauthStrategy = strategyFactory.GetStrategy(consent.InstitutionId);

        var tokenResult = await oauthStrategy.RefreshTokenAsync(
            consent.Token.RefreshToken,
            cancellationToken);

        consent.RotateTokens(
            tokenResult.AccessToken,
            tokenResult.RefreshToken,
            tokenResult.ExpiresInSeconds,
            timeProvider);

        await repository.UpdateAsync(consent, cancellationToken);

        return tokenResult;
    }
}
