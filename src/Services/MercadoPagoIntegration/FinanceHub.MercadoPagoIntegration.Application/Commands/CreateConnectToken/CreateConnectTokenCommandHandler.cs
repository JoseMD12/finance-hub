using FinanceHub.MercadoPagoIntegration.Application.DTOs;
using FinanceHub.MercadoPagoIntegration.Application.Interfaces;
using FinanceHub.MercadoPagoIntegration.Domain.Exceptions;
using Microsoft.Extensions.Logging;

namespace FinanceHub.MercadoPagoIntegration.Application.Commands.CreateConnectToken;

public class CreateConnectTokenCommandHandler : ICreateConnectTokenCommandHandler
{
    private readonly IOpenFinanceClient _openFinanceClient;
    private readonly ILogger<CreateConnectTokenCommandHandler> _logger;

    public CreateConnectTokenCommandHandler(
        IOpenFinanceClient openFinanceClient,
        ILogger<CreateConnectTokenCommandHandler> logger)
    {
        _openFinanceClient = openFinanceClient;
        _logger = logger;
    }

    public async Task<ConnectTokenDto> Handle(CreateConnectTokenCommand command, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(command.UserId))
        {
            throw new NullOrEmptyMercadoPagoCredentialsDomainException("UserId não pode ser nulo ou vazio para gerar ConnectToken.");
        }

        _logger.LogInformation("Gerando ConnectToken Open Finance para o usuário {UserId} (ItemId: {ItemId})", command.UserId, command.ItemId);
        return await _openFinanceClient.CreateConnectTokenAsync(command.ItemId, ct);
    }
}
