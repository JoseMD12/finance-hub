namespace FinanceHub.PluggyIntegration.Domain.Exceptions;

public class PluggyRateLimitDomainException : DomainException
{
    public PluggyRateLimitDomainException(string? message = null)
        : base(
            message ?? "Limite de requisições excedido na API da Pluggy.",
            "PLUGGY_RATE_LIMIT_EXCEEDED",
            statusCode: 429)
    {
    }
}
