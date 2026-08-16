using FinanceHub.MercadoPagoIntegration.Application.DTOs;
using FinanceHub.Shared.Connectors;

namespace FinanceHub.MercadoPagoIntegration.Application.Interfaces;

public interface IOpenFinanceClient
{
    Task<ConnectTokenDto> CreateConnectTokenAsync(string? itemId = null, CancellationToken ct = default);
    Task<IReadOnlyCollection<BankAccountDto>> GetAccountsByItemAsync(string itemId, CancellationToken ct = default);
    Task<IReadOnlyCollection<BankTransactionDto>> GetTransactionsByAccountAsync(
        string accountId,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken ct = default);
}
