namespace FinanceHub.AuthConsent.Application.Commands.RevokeConsent;

public interface IRevokeConsentCommandHandler
{
    Task Handle(RevokeConsentCommand command, CancellationToken cancellationToken);
}
