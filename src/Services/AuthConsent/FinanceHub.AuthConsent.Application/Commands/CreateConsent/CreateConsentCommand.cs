namespace FinanceHub.AuthConsent.Application.Commands.CreateConsent;

public record CreateConsentCommand(
    string UserId,
    string InstitutionId,
    string ExternalConsentId);
