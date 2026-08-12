namespace FinanceHub.AuthConsent.Domain.Exceptions;

public class ConsentDomainException : DomainException
{
    public ConsentDomainException(string message, string errorCode = "CONSENT_DOMAIN_ERROR", int statusCode = 400)
        : base(message, errorCode, statusCode)
    {
    }

    public ConsentDomainException(string message, Exception innerException, string errorCode = "CONSENT_DOMAIN_ERROR", int statusCode = 400)
        : base(message, innerException, errorCode, statusCode)
    {
    }
}
