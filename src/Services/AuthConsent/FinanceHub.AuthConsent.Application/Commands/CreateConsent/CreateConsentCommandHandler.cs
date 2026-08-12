using FinanceHub.AuthConsent.Application.Interfaces;
using FinanceHub.AuthConsent.Domain.Entities;

namespace FinanceHub.AuthConsent.Application.Commands.CreateConsent;

public sealed class CreateConsentCommandHandler(
    IBankConsentRepository repository,
    TimeProvider timeProvider) : ICreateConsentCommandHandler
{
    public async Task<Guid> Handle(CreateConsentCommand command, CancellationToken cancellationToken)
    {
        var consent = BankConsent.Request(
            command.UserId,
            command.InstitutionId,
            command.ExternalConsentId,
            timeProvider);

        await repository.AddAsync(consent, cancellationToken);

        return consent.Id;
    }
}
