using FinanceHub.AuthConsent.Application.DTOs;
using FinanceHub.AuthConsent.Application.Exceptions;
using FinanceHub.AuthConsent.Application.Interfaces;
using FinanceHub.Shared.Messaging.Events;

namespace FinanceHub.AuthConsent.Application.Commands.AuthorizeConsent;

public sealed class AuthorizeConsentCommandHandler(
    IBankConsentRepository repository,
    IKeyedOAuthStrategyFactory strategyFactory,
    IEventPublisher eventPublisher,
    TimeProvider timeProvider)
{
    public async Task<ConsentResponseDto> Handle(AuthorizeConsentCommand command, CancellationToken cancellationToken = default)
    {
        var consent = await repository.GetByIdAsync(command.ConsentId, cancellationToken)
                      ?? throw new ConsentNotFoundDomainException(command.ConsentId);

        var strategy = strategyFactory.GetStrategy(consent.InstitutionId);

        var tokenResult = await strategy.ExchangeCodeForTokensAsync(command.AuthCode, command.RedirectUri, cancellationToken);

        consent.Authorize(
            accessToken: tokenResult.AccessToken,
            refreshToken: tokenResult.RefreshToken,
            expiresInSeconds: tokenResult.ExpiresInSeconds,
            timeProvider: timeProvider
        );

        await repository.UpdateAsync(consent, cancellationToken);

        var linkedEvent = new BankAccountLinked(
            LinkId: Guid.NewGuid(),
            InstitutionId: consent.InstitutionId,
            UserId: consent.UserId,
            ConsentId: consent.Id.ToString(),
            LinkedAtUtc: timeProvider.GetUtcNow().UtcDateTime
        );

        await eventPublisher.PublishAsync(linkedEvent, cancellationToken);

        return new ConsentResponseDto(
            ConsentId: consent.Id,
            UserId: consent.UserId,
            InstitutionId: consent.InstitutionId,
            Status: consent.Status.ToString(),
            ExpiresAtUtc: consent.Token.ExpiresAtUtc,
            CreatedAtUtc: consent.CreatedAtUtc
        );
    }
}
