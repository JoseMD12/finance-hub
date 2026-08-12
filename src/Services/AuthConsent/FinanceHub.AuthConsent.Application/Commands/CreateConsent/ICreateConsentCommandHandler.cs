namespace FinanceHub.AuthConsent.Application.Commands.CreateConsent;

public interface ICreateConsentCommandHandler
{
    Task<Guid> Handle(CreateConsentCommand command, CancellationToken cancellationToken);
}
