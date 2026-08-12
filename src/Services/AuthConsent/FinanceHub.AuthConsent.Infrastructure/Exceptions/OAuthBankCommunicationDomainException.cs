using FinanceHub.AuthConsent.Domain.Exceptions;

namespace FinanceHub.AuthConsent.Infrastructure.Exceptions;

public class OAuthBankCommunicationDomainException : DomainException
{
    public OAuthBankCommunicationDomainException(string institutionId, string details)
        : base($"Falha na comunicação de autorização com a instituição bancária '{institutionId}': {details}", "OAUTH_BANK_COMMUNICATION_ERROR", statusCode: 502)
    {
    }
}
