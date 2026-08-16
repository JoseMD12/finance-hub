using FinanceHub.MercadoPagoIntegration.Application.DTOs;
using FinanceHub.MercadoPagoIntegration.Application.Interfaces;
using FinanceHub.MercadoPagoIntegration.Domain.Constants;
using FinanceHub.MercadoPagoIntegration.Domain.Entities;
using FinanceHub.MercadoPagoIntegration.Domain.Exceptions;
using FinanceHub.Shared.Messaging.Events;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace FinanceHub.MercadoPagoIntegration.Application.Commands.SyncTransactions;

public class SyncMercadoPagoOpenFinanceCommandHandler : ISyncMercadoPagoOpenFinanceCommandHandler
{
    private readonly IOpenFinanceClient _openFinanceClient;
    private readonly IMercadoPagoSyncStateRepository _syncStateRepository;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<SyncMercadoPagoOpenFinanceCommandHandler> _logger;

    public SyncMercadoPagoOpenFinanceCommandHandler(
        IOpenFinanceClient openFinanceClient,
        IMercadoPagoSyncStateRepository syncStateRepository,
        IPublishEndpoint publishEndpoint,
        TimeProvider timeProvider,
        ILogger<SyncMercadoPagoOpenFinanceCommandHandler> logger)
    {
        _openFinanceClient = openFinanceClient;
        _syncStateRepository = syncStateRepository;
        _publishEndpoint = publishEndpoint;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task<OpenFinanceSyncResultDto> Handle(SyncMercadoPagoOpenFinanceCommand command, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(command.UserId))
        {
            throw new NullOrEmptyMercadoPagoCredentialsDomainException("UserId não pode ser nulo ou vazio para sincronização.");
        }

        if (string.IsNullOrWhiteSpace(command.ItemId))
        {
            throw new NullOrEmptyMercadoPagoCredentialsDomainException("ItemId da conexão Open Finance não pode ser nulo ou vazio.");
        }

        _logger.LogInformation("Iniciando sincronização Open Finance do Mercado Pago para o usuário {UserId} (Item: {ItemId})", command.UserId, command.ItemId);

        // 1. Obter contas vinculadas ao item
        var accounts = await _openFinanceClient.GetAccountsByItemAsync(command.ItemId, ct);
        if (accounts.Count == 0)
        {
            throw new OpenFinanceItemNotFoundDomainException(command.ItemId);
        }

        var totalIngested = 0;
        var now = _timeProvider.GetUtcNow();
        var latestCursor = now.UtcDateTime;

        foreach (var account in accounts)
        {
            if (!string.IsNullOrWhiteSpace(command.AccountId) && !string.Equals(account.AccountId, command.AccountId, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var syncState = await _syncStateRepository.GetByAccountAsync(command.UserId, account.AccountId, ct);
            var isInitial = syncState == null;

            var from = isInitial
                ? now.AddDays(-MercadoPagoConstants.DefaultInitialSyncDays)
                : new DateTimeOffset(syncState!.LastSyncCursorUtc, TimeSpan.Zero).AddHours(-MercadoPagoConstants.SafetyOverlapHours);

            var to = now;

            if (syncState == null)
            {
                syncState = MercadoPagoSyncState.Create(command.UserId, account.AccountId, from.UtcDateTime, _timeProvider);
                await _syncStateRepository.AddAsync(syncState, ct);
            }

            syncState.StartSync(_timeProvider);

            try
            {
                var transactions = await _openFinanceClient.GetTransactionsByAccountAsync(account.AccountId, from, to, ct);

                foreach (var tx in transactions)
                {
                    var ingestedEvent = new TransactionIngested(
                        IngestionId: Guid.NewGuid(),
                        UserId: command.UserId,
                        Source: MercadoPagoConstants.BankIdentifier,
                        AccountId: tx.AccountId,
                        BankTransactionId: tx.TransactionId,
                        Amount: tx.Amount,
                        TransactionDate: tx.BookingDateTime.UtcDateTime,
                        Description: tx.TransactionInformation,
                        Currency: tx.Currency,
                        RawPayloadJson: tx.RawPayload,
                        OccurredAtUtc: _timeProvider.GetUtcNow().UtcDateTime
                    );

                    await _publishEndpoint.Publish(ingestedEvent, ct);
                    totalIngested++;
                }

                syncState.CompleteSync(to.UtcDateTime, transactions.Count, _timeProvider);
                latestCursor = syncState.LastSyncCursorUtc;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Falha durante sincronização da conta {AccountId} do Mercado Pago", account.AccountId);
                syncState.FailSync(ex.Message, _timeProvider);
                await _syncStateRepository.SaveChangesAsync(ct);
                throw;
            }
        }

        await _syncStateRepository.SaveChangesAsync(ct);

        _logger.LogInformation("Sincronização Open Finance concluída com sucesso: {Count} transações ingeridas", totalIngested);

        return new OpenFinanceSyncResultDto(
            SyncId: Guid.NewGuid(),
            Status: "Completed",
            IngestedCount: totalIngested,
            LastSyncCursorUtc: latestCursor
        );
    }
}
