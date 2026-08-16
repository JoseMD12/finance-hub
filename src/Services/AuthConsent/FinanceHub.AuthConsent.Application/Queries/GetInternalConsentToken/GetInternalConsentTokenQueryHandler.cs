using FinanceHub.AuthConsent.Application.DTOs;
using FinanceHub.AuthConsent.Application.Interfaces;
using FinanceHub.AuthConsent.Domain.Entities;

namespace FinanceHub.AuthConsent.Application.Queries.GetInternalConsentToken;

public class GetInternalConsentTokenQueryHandler : IGetInternalConsentTokenQueryHandler
{
    private readonly IBankConsentRepository _repository;

    public GetInternalConsentTokenQueryHandler(IBankConsentRepository repository)
    {
        _repository = repository;
    }

    public async Task<InternalConsentTokenDto?> Handle(GetInternalConsentTokenQuery query, CancellationToken cancellationToken = default)
    {
        var consents = await _repository.GetByUserIdAsync(query.UserId, cancellationToken);
        var consent = consents
            .Where(c => string.Equals(c.InstitutionId, query.InstitutionId, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(c => c.CreatedAtUtc)
            .FirstOrDefault();

        if (consent is null)
        {
            return null;
        }

        var isAuthorised = consent.Status == ConsentStatus.Authorized;
        var accessToken = consent.Token.AccessToken ?? "";
        var refreshToken = consent.Token.RefreshToken;
        
        var expiresInSeconds = 3600;
        if (consent.Token.ExpiresAtUtc.HasValue)
        {
            var remaining = (int)(consent.Token.ExpiresAtUtc.Value - DateTime.UtcNow).TotalSeconds;
            expiresInSeconds = Math.Max(0, remaining);
        }

        return new InternalConsentTokenDto(accessToken, refreshToken, expiresInSeconds, isAuthorised);
    }
}
