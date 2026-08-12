namespace FinanceHub.AuthConsent.Application.DTOs;

public record ConsentResponseDto(
    Guid ConsentId,
    string UserId,
    string InstitutionId,
    string Status,
    DateTime? ExpiresAtUtc,
    DateTime CreatedAtUtc
);
