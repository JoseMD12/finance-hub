namespace FinanceHub.MercadoPagoIntegration.Domain.Exceptions;

public class OpenFinanceAuthenticationDomainException : DomainException
{
    public OpenFinanceAuthenticationDomainException(string message = "Falha de autenticação com o provedor Open Finance. Verifique suas credenciais.")
        : base(message, "OPENFINANCE_UNAUTHORIZED", statusCode: 401)
    {
    }
}
