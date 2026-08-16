namespace FinanceHub.MercadoPagoIntegration.Domain.Exceptions;

public class OpenFinanceConsentRevokedDomainException : DomainException
{
    public OpenFinanceConsentRevokedDomainException(string status)
        : base($"O consentimento Open Finance do Mercado Pago encontra-se no estado '{status}' e não permite sincronização.", "OPENFINANCE_CONSENT_REVOKED", statusCode: 409)
    {
    }
}
