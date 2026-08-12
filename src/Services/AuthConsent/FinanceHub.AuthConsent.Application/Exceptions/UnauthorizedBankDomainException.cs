using FinanceHub.AuthConsent.Domain.Exceptions;

namespace FinanceHub.AuthConsent.Application.Exceptions;

public class UnauthorizedBankDomainException : DomainException
{
    public UnauthorizedBankDomainException(string institutionId)
        : base($"Falha na comunicação de autorização com a instituição bancária '{institutionId}'.", "UNAUTHORIZED_BANK_ACCESS", statusCode: 401)
    {
    }
}
