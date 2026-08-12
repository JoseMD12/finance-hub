namespace FinanceHub.AuthConsent.Domain.Exceptions;

public class NullOrEmptyInstitutionIdDomainException : DomainException
{
    public NullOrEmptyInstitutionIdDomainException(string? institutionId = null)
        : base(
            string.IsNullOrWhiteSpace(institutionId)
                ? "InstitutionId não pode ser nulo ou vazio."
                : $"Instituição bancária '{institutionId}' não é suportada.",
            "NULL_OR_EMPTY_INSTITUTION_ID",
            statusCode: 400)
    {
    }
}
