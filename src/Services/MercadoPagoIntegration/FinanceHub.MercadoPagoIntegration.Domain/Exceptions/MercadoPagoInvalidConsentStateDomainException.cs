namespace FinanceHub.MercadoPagoIntegration.Domain.Exceptions;

public class MercadoPagoInvalidConsentStateDomainException : DomainException
{
    public MercadoPagoInvalidConsentStateDomainException(string currentStatus)
        : base($"O consentimento do Mercado Pago encontra-se no estado '{currentStatus}' e não pode realizar sincronização.", "MERCADO_PAGO_CONSENT_INVALID_STATE", statusCode: 409)
    {
    }
}
