namespace FinanceHub.AuthConsent.Domain.Exceptions;

public class NullOrEmptyAccessTokenDomainException : DomainException
{
    public NullOrEmptyAccessTokenDomainException()
        : base("AccessToken não pode ser vazio para autorização.", "NULL_OR_EMPTY_ACCESS_TOKEN", statusCode: 400)
    {
    }
}
