using FinanceHub.AuthConsent.Application.DTOs;

namespace FinanceHub.AuthConsent.Application.Commands.RenewToken;

public interface IRenewTokenCommandHandler
{
    Task<OAuthTokenExchangeResult> Handle(RenewTokenCommand command, CancellationToken cancellationToken);
}
