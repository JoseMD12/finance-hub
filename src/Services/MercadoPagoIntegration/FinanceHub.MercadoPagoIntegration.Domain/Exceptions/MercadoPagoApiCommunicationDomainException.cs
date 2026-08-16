namespace FinanceHub.MercadoPagoIntegration.Domain.Exceptions;

public class MercadoPagoApiCommunicationDomainException : DomainException
{
    public MercadoPagoApiCommunicationDomainException(string message, int statusCode = 502)
        : base(message, "MERCADO_PAGO_GATEWAY_ERROR", statusCode)
    {
    }

    public MercadoPagoApiCommunicationDomainException(string message, Exception innerException, int statusCode = 502)
        : base(message, innerException, "MERCADO_PAGO_GATEWAY_ERROR", statusCode)
    {
    }
}
