using FinanceHub.AuthConsent.Application.DTOs;
using FinanceHub.AuthConsent.Application.Interfaces;

namespace FinanceHub.AuthConsent.Application.Queries.GetConsentByUserId;

public sealed class GetConsentByUserIdQueryHandler(IBankConsentRepository repository) : IGetConsentByUserIdQueryHandler
{
    public async Task<IEnumerable<ConsentResponseDto>> Handle(GetConsentByUserIdQuery query, CancellationToken cancellationToken)
    {
        var consents = await repository.GetByUserIdAsync(query.UserId, cancellationToken);

        return consents.Select(c => new ConsentResponseDto(
            c.Id,
            c.UserId,
            c.InstitutionId,
            c.Status.ToString(),
            c.Token.ExpiresAtUtc,
            c.CreatedAtUtc));
    }
}
