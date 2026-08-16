namespace FinanceHub.MercadoPagoIntegration.Application.DTOs;

public record ConnectTokenDto(
    string AccessToken,
    DateTime ExpiresAtUtc
);
