using FinanceHub.AuthConsent.Application.DTOs;

namespace FinanceHub.AuthConsent.Application.Commands.AuthorizeConsent;

public interface IAuthorizeConsentCommandHandler
{
    Task<ConsentResponseDto> Handle(AuthorizeConsentCommand command, CancellationToken cancellationToken);
}
