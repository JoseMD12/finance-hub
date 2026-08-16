using FinanceHub.MercadoPagoIntegration.Application.DTOs;

namespace FinanceHub.MercadoPagoIntegration.Application.Commands.SyncTransactions;

public interface ISyncMercadoPagoOpenFinanceCommandHandler
{
    Task<OpenFinanceSyncResultDto> Handle(SyncMercadoPagoOpenFinanceCommand command, CancellationToken ct = default);
}
