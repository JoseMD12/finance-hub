namespace FinanceHub.MercadoPagoIntegration.Application.Commands.CreateConnectToken;

public record CreateConnectTokenCommand(
    string UserId,
    string? ItemId = null
);
