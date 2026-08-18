using FinanceHub.PluggyIntegration.Domain.Constants;

namespace FinanceHub.PluggyIntegration.Domain.Exceptions;

public class NullOrEmptyPluggyAccessTokenDomainException : DomainException
{
    public NullOrEmptyPluggyAccessTokenDomainException()
        : base(
            $"O token de acesso do Meu.Pluggy (pluggyAccessToken / {PluggyConstants.HeaderNames.PluggyAccessToken}) é obrigatório para realizar a sincronização.",
            "NULL_OR_EMPTY_PLUGGY_ACCESS_TOKEN",
            statusCode: 400)
    {
    }
}
