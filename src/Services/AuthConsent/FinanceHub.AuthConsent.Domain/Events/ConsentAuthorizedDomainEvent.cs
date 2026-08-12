namespace FinanceHub.AuthConsent.Domain.Events;

public record ConsentAuthorizedDomainEvent(
    Guid ConsentId,
    string UserId,
    string InstitutionId,
    DateTime AuthorizedAtUtc
);
