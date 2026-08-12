namespace FinanceHub.AuthConsent.Domain.Exceptions;

public class NullOrEmptyExternalConsentIdDomainException : DomainException
{
    public NullOrEmptyExternalConsentIdDomainException()
        : base("ExternalConsentId não pode ser nulo ou vazio.", "NULL_OR_EMPTY_EXTERNAL_CONSENT_ID", statusCode: 400)
    {
    }
}
