using FinanceHub.AuthConsent.Application.DTOs;

namespace FinanceHub.AuthConsent.Application.Commands.AuthorizeConsent;

public record AuthorizeConsentCommand(
    Guid ConsentId,
    string AuthCode,
    string RedirectUri
);
