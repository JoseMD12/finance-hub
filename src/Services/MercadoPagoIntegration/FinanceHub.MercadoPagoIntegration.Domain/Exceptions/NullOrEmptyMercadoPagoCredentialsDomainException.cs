namespace FinanceHub.MercadoPagoIntegration.Domain.Exceptions;

public class NullOrEmptyMercadoPagoCredentialsDomainException : DomainException
{
    public NullOrEmptyMercadoPagoCredentialsDomainException(string paramName = "Credentials")
        : base($"As credenciais do Mercado Pago '{paramName}' não podem ser nulas ou vazias.", "INVALID_MERCADO_PAGO_CREDENTIALS", statusCode: 400)
    {
    }
}
