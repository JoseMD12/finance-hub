namespace FinanceHub.AuthConsent.Domain.Exceptions;

public class ConsentInvalidStateException : DomainException
{
    public ConsentInvalidStateException(string currentStatus, string targetAction)
        : base(
            $"Consentimento no estado '{currentStatus}' não pode executar a ação '{targetAction}'.",
            "CONSENT_INVALID_STATE",
            statusCode: 409)
    {
    }
}
