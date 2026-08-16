namespace FinanceHub.MercadoPagoIntegration.Domain.Exceptions;

public class OpenFinanceRateLimitExceededDomainException : DomainException
{
    public int RetryAfterSeconds { get; }

    public OpenFinanceRateLimitExceededDomainException(int retryAfterSeconds = 60)
        : base($"Limite de requisições excedido com o provedor Open Finance. Tente novamente em {retryAfterSeconds} segundos.", "OPENFINANCE_RATE_LIMIT_EXCEEDED", statusCode: 429)
    {
        RetryAfterSeconds = retryAfterSeconds;
    }
}
