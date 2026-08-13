using FinanceHub.ApiGateway.DTOs;

namespace FinanceHub.ApiGateway.Clients;

public interface IAuthConsentServiceClient
{
    Task<IEnumerable<GatewayConsentDto>> GetConsentsByUserIdAsync(string userId, CancellationToken ct = default);
    Task<Guid> CreateConsentAsync(string userId, string institutionId, string externalConsentId, CancellationToken ct = default);
    Task<GatewayConsentDto> AuthorizeConsentAsync(Guid consentId, string authCode, string redirectUri, CancellationToken ct = default);
    Task RevokeConsentAsync(Guid consentId, CancellationToken ct = default);
    Task<bool> HealthCheckAsync(CancellationToken ct = default);
}
