namespace FinanceHub.AuthConsent.Domain.Exceptions;

public class NullOrEmptyUserIdDomainException : DomainException
{
    public NullOrEmptyUserIdDomainException(string? userId = null)
        : base(
            string.IsNullOrWhiteSpace(userId)
                ? "UserId não pode ser nulo ou vazio."
                : $"UserId '{userId}' não é válido.",
            "NULL_OR_EMPTY_USER_ID",
            statusCode: 400)
    {
    }
}
