namespace FinanceHub.MercadoPagoIntegration.Domain.Exceptions;

public class MercadoPagoAccountNotFoundDomainException : DomainException
{
    public MercadoPagoAccountNotFoundDomainException(string accountId)
        : base($"A conta do Mercado Pago '{accountId}' não foi localizada.", "MERCADO_PAGO_ACCOUNT_NOT_FOUND", statusCode: 404)
    {
    }
}
