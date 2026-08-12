using FinanceHub.AuthConsent.Application.DTOs;
using FinanceHub.AuthConsent.Application.Exceptions;
using FinanceHub.AuthConsent.Application.Interfaces;
using FinanceHub.Shared.Messaging.Events;

namespace FinanceHub.AuthConsent.Application.Commands.AuthorizeConsent;

public sealed class AuthorizeConsentCommandHandler(
    IBankConsentRepository repository,
    IKeyedOAuthStrategyFactory strategyFactory,
    IEventPublisher eventPublisher,
    TimeProvider timeProvider) : IAuthorizeConsentCommandHandler
{
    public async Task<ConsentResponseDto> Handle(AuthorizeConsentCommand command, CancellationToken cancellationToken)
    {
        var consent = await repository.GetByIdAsync(command.ConsentId, cancellationToken)
                      ?? throw new ConsentNotFoundDomainException(command.ConsentId);

        var oauthStrategy = strategyFactory.GetStrategy(consent.InstitutionId);

        var tokenResult = await oauthStrategy.ExchangeCodeForTokensAsync(
            command.AuthCode,
            command.RedirectUri,
            cancellationToken);

        consent.Authorize(
            tokenResult.AccessToken,
            tokenResult.RefreshToken,
            tokenResult.ExpiresInSeconds,
            timeProvider);

        await repository.UpdateAsync(consent, cancellationToken);

        var bankAccountLinkedEvent = new BankAccountLinked(
            Guid.NewGuid(),
            consent.InstitutionId,
            consent.UserId,
            consent.Id.ToString(),
            timeProvider.GetUtcNow().UtcDateTime);

        await eventPublisher.PublishAsync(bankAccountLinkedEvent, cancellationToken);

        return new ConsentResponseDto(
            consent.Id,
            consent.UserId,
            consent.InstitutionId,
            consent.Status.ToString(),
            consent.Token.ExpiresAtUtc,
            consent.CreatedAtUtc);
    }
}
