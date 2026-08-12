namespace FinanceHub.AuthConsent.Domain.Exceptions;

public class NullOrEmptyRefreshTokenDomainException : DomainException
{
    public NullOrEmptyRefreshTokenDomainException()
        : base("RefreshToken não pode ser vazio para autorização.", "NULL_OR_EMPTY_REFRESH_TOKEN", statusCode: 400)
    {
    }
}
