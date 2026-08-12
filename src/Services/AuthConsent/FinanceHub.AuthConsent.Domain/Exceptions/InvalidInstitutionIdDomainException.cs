namespace FinanceHub.AuthConsent.Domain.Exceptions;

public class InvalidInstitutionIdDomainException : DomainException
{
    public InvalidInstitutionIdDomainException(string? institutionId = null)
        : base(
            string.IsNullOrWhiteSpace(institutionId)
                ? "InstitutionId não pode ser nulo ou vazio."
                : $"Instituição bancária '{institutionId}' não é suportada ou é inválida.",
            "INVALID_INSTITUTION_ID",
            statusCode: 400)
    {
    }
}
