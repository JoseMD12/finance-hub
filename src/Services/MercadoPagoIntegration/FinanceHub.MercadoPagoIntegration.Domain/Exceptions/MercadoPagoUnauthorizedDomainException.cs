namespace FinanceHub.MercadoPagoIntegration.Domain.Exceptions;

public class MercadoPagoUnauthorizedDomainException : DomainException
{
    public MercadoPagoUnauthorizedDomainException(string message = "Token de acesso do Mercado Pago expirado, revogado ou inválido.")
        : base(message, "MERCADO_PAGO_UNAUTHORIZED", statusCode: 401)
    {
    }
}
