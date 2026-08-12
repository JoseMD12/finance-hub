using FinanceHub.AuthConsent.Domain.Exceptions;

namespace FinanceHub.AuthConsent.Application.Exceptions;

public class ConsentNotFoundDomainException : DomainException
{
    public ConsentNotFoundDomainException(Guid consentId)
        : base($"Consentimento '{consentId}' não foi localizado no repositório.", "CONSENT_NOT_FOUND", statusCode: 404)
    {
    }
}
