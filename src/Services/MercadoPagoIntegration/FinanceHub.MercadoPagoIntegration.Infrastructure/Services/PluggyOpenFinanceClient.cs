using FinanceHub.MercadoPagoIntegration.Application.DTOs;
using FinanceHub.MercadoPagoIntegration.Application.Interfaces;
using FinanceHub.MercadoPagoIntegration.Infrastructure.Configuration;
using FinanceHub.Shared.Connectors;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FinanceHub.MercadoPagoIntegration.Infrastructure.Services;

public class PluggyOpenFinanceClient : IOpenFinanceClient
{
    public PluggyOpenFinanceClient()
    {
    }

    public PluggyOpenFinanceClient(
        HttpClient httpClient,
        IOptions<OpenFinanceOptions> options,
        ILogger<PluggyOpenFinanceClient> logger)
    {
    }

    public Task<ConnectTokenDto> CreateConnectTokenAsync(string? itemId = null, CancellationToken ct = default)
    {
        throw new NotImplementedException("Integração do Mercado Pago via Open Finance está em Stand-by.");
    }

    public Task<IReadOnlyCollection<BankAccountDto>> GetAccountsByItemAsync(string itemId, CancellationToken ct = default)
    {
        throw new NotImplementedException("Integração do Mercado Pago via Open Finance está em Stand-by.");
    }

    public Task<IReadOnlyCollection<BankTransactionDto>> GetTransactionsByAccountAsync(
        string accountId,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken ct = default)
    {
        throw new NotImplementedException("Integração do Mercado Pago via Open Finance está em Stand-by.");
    }
}
