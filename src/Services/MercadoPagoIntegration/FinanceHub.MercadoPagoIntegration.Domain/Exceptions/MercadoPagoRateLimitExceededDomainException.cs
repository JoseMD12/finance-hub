namespace FinanceHub.MercadoPagoIntegration.Domain.Exceptions;

public class MercadoPagoRateLimitExceededDomainException : DomainException
{
    public MercadoPagoRateLimitExceededDomainException(int? retryAfterSeconds = null)
        : base(
            retryAfterSeconds.HasValue
                ? $"Limite de requisições do Mercado Pago excedido. Tente novamente em {retryAfterSeconds.Value} segundos."
                : "Limite de requisições do Mercado Pago excedido (HTTP 429).",
            "MERCADO_PAGO_RATE_LIMIT_EXCEEDED",
            statusCode: 429)
    {
    }
}
