namespace FinanceHub.MercadoPagoIntegration.Domain.Exceptions;

public class OpenFinanceServiceDomainException : DomainException
{
    public OpenFinanceServiceDomainException(string message, Exception? innerException = null)
        : base(
            message,
            innerException ?? new Exception(message),
            "OPENFINANCE_GATEWAY_ERROR",
            statusCode: 502)
    {
    }
}
