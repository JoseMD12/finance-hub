using FinanceHub.AuthConsent.Application.Exceptions;
using FinanceHub.AuthConsent.Application.Interfaces;

namespace FinanceHub.AuthConsent.Application.Commands.RevokeConsent;

public sealed class RevokeConsentCommandHandler(
    IBankConsentRepository repository,
    TimeProvider timeProvider) : IRevokeConsentCommandHandler
{
    public async Task Handle(RevokeConsentCommand command, CancellationToken cancellationToken)
    {
        var consent = await repository.GetByIdAsync(command.ConsentId, cancellationToken)
                      ?? throw new ConsentNotFoundDomainException(command.ConsentId);

        consent.Revoke(timeProvider);

        await repository.UpdateAsync(consent, cancellationToken);
    }
}
