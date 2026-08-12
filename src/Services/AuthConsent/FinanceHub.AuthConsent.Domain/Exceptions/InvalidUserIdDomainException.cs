namespace FinanceHub.AuthConsent.Domain.Exceptions;

public class InvalidUserIdDomainException : DomainException
{
    public InvalidUserIdDomainException(string? userId = null)
        : base(
            string.IsNullOrWhiteSpace(userId)
                ? "UserId não pode ser nulo ou vazio."
                : $"UserId '{userId}' não é válido.",
            "INVALID_USER_ID",
            statusCode: 400)
    {
    }
}
