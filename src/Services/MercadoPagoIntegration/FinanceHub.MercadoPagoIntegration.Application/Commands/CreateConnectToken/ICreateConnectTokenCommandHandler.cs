using FinanceHub.MercadoPagoIntegration.Application.DTOs;

namespace FinanceHub.MercadoPagoIntegration.Application.Commands.CreateConnectToken;

public interface ICreateConnectTokenCommandHandler
{
    Task<ConnectTokenDto> Handle(CreateConnectTokenCommand command, CancellationToken ct = default);
}
