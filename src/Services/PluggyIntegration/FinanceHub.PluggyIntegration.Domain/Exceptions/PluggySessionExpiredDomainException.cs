namespace FinanceHub.PluggyIntegration.Domain.Exceptions;

public class PluggySessionExpiredDomainException : DomainException
{
    public PluggySessionExpiredDomainException(string? detail = null)
        : base(
            string.IsNullOrWhiteSpace(detail)
                ? "A sessão com a API do Meu.Pluggy expirou. Atualize o token PLUGGY_USER_TOKEN para continuar."
                : $"A sessão do Meu.Pluggy expirou: {detail}",
            "PLUGGY_SESSION_EXPIRED",
            statusCode: 401)
    {
    }
}
