namespace FinanceHub.ApiGateway.DTOs;

public record GatewayConsentDto(
    Guid ConsentId,
    string UserId,
    string InstitutionId,
    string ExternalConsentId,
    string Status,
    DateTime CreatedAtUtc,
    DateTime? ExpiresAtUtc);

public record GatewayCreateConsentRequest(
    string InstitutionId,
    string ExternalConsentId);

public record GatewayAuthorizeConsentRequest(
    string AuthCode,
    string RedirectUri);
