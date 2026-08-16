using FinanceHub.AuthConsent.Application.DTOs;

namespace FinanceHub.AuthConsent.Application.Queries.GetInternalConsentToken;

public interface IGetInternalConsentTokenQueryHandler
{
    Task<InternalConsentTokenDto?> Handle(GetInternalConsentTokenQuery query, CancellationToken cancellationToken = default);
}
