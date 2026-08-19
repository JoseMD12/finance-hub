using FinanceHub.PluggyIntegration.Application.DTOs;
using FinanceHub.PluggyIntegration.Application.Interfaces;
using FinanceHub.PluggyIntegration.Application.Services;
using FinanceHub.PluggyIntegration.Domain.Exceptions;
using FinanceHub.Shared.Messaging.Events;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace FinanceHub.PluggyIntegration.Application.Commands.SyncAllPluggyAccounts;

public sealed class SyncAllPluggyAccountsCommandHandler(
    IMeuPluggyClient pluggyClient,
    IPluggyAggregationService aggregationService,
    IPluggyTransactionMapper transactionMapper,
    IPublishEndpoint publishEndpoint,
    ILogger<SyncAllPluggyAccountsCommandHandler> logger) : ISyncAllPluggyAccountsCommandHandler
{
    public async Task<SyncPluggySummaryDto> HandleAsync(SyncAllPluggyAccountsCommand command, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.PluggyAccessToken))
        {
            throw new NullOrEmptyPluggyAccessTokenDomainException();
        }

        logger.LogInformation("Iniciando sincronização unificada em lote via Meu.Pluggy para UserId: {UserId}", command.UserId);

        var itemsTask = pluggyClient.GetItemsAsync(command.PluggyAccessToken, cancellationToken);
        var accountsTask = aggregationService.FetchAllAccountsAsync(command.PluggyAccessToken, cancellationToken);

        await Task.WhenAll(itemsTask, accountsTask);

        var items = await itemsTask;
        var accounts = await accountsTask;

        if (items.Count == 0 || accounts.Count == 0)
        {
            logger.LogInformation("Sincronização concluída com zero itens ou contas para sincronizar.");
            return new SyncPluggySummaryDto(
                TotalItemsSynced: items.Count,
                TotalAccountsSynced: accounts.Count,
                TotalCheckingTransactionsIngested: 0,
                TotalCardTransactionsIngested: 0,
                SyncedAtUtc: DateTime.UtcNow
            );
        }

        var allTransactions = await aggregationService.FetchAllTransactionsAsync(command.PluggyAccessToken, cancellationToken);

        var itemMap = items.ToDictionary(i => i.Id, StringComparer.OrdinalIgnoreCase);
        var accountMap = accounts.ToDictionary(a => a.Id, StringComparer.OrdinalIgnoreCase);

        var checkingEvents = new List<TransactionIngested>();
        var cardEvents = new List<InvoiceItemIngested>();

        foreach (var tx in allTransactions)
        {
            if (string.IsNullOrWhiteSpace(tx.AccountId) || !accountMap.TryGetValue(tx.AccountId, out var account))
            {
                continue;
            }

            var item = itemMap.GetValueOrDefault(account.ItemId);
            var sourceName = item?.Connector.Name ?? account.Name;
            transactionMapper.MapTransactionToEvents(tx, account, sourceName, command.UserId, checkingEvents, cardEvents);
        }

        await PublishEventsAsync(checkingEvents, cardEvents, cancellationToken);

        int totalCheckingTxs = checkingEvents.Count;
        int totalCardTxs = cardEvents.Count;

        logger.LogInformation("Sincronização em lote concluída: {Items} items, {Accounts} contas, {CheckingTxs} txs de conta corrente, {CardTxs} txs de cartão de crédito.",
            items.Count, accounts.Count, totalCheckingTxs, totalCardTxs);

        return new SyncPluggySummaryDto(
            TotalItemsSynced: items.Count,
            TotalAccountsSynced: accounts.Count,
            TotalCheckingTransactionsIngested: totalCheckingTxs,
            TotalCardTransactionsIngested: totalCardTxs,
            SyncedAtUtc: DateTime.UtcNow
        );
    }

    private async Task PublishEventsAsync(
        IReadOnlyList<TransactionIngested> checkingEvents,
        IReadOnlyList<InvoiceItemIngested> cardEvents,
        CancellationToken cancellationToken)
    {
        var publishTasks = new List<Task>(checkingEvents.Count + cardEvents.Count);

        foreach (var checkingEvent in checkingEvents)
        {
            publishTasks.Add(publishEndpoint.Publish(checkingEvent, cancellationToken));
        }

        foreach (var cardEvent in cardEvents)
        {
            publishTasks.Add(publishEndpoint.Publish(cardEvent, cancellationToken));
        }

        await Task.WhenAll(publishTasks);
    }
}
