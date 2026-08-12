using FinanceHub.AuthConsent.Application.DTOs;

namespace FinanceHub.AuthConsent.Application.Queries.GetConsentByUserId;

public interface IGetConsentByUserIdQueryHandler
{
    Task<IEnumerable<ConsentResponseDto>> Handle(GetConsentByUserIdQuery query, CancellationToken cancellationToken);
}
