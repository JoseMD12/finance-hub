using FinanceHub.PluggyIntegration.Application.Commands.SyncAllPluggyAccounts;
using FinanceHub.PluggyIntegration.Application.DTOs;
using FinanceHub.PluggyIntegration.Application.Interfaces;
using FinanceHub.PluggyIntegration.Application.Services;
using FinanceHub.PluggyIntegration.Domain.Exceptions;
using FinanceHub.Shared.Messaging.Events;
using MassTransit;

namespace FinanceHub.PluggyIntegration.Application.Commands.SyncSinglePluggyItem;

public sealed class SyncSinglePluggyItemCommandHandler(
    IMeuPluggyClient pluggyClient,
    IPluggyTransactionMapper transactionMapper,
    IPublishEndpoint publishEndpoint) : ISyncSinglePluggyItemCommandHandler
{
    public async Task<SyncPluggySummaryDto> HandleAsync(SyncSinglePluggyItemCommand command, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.PluggyAccessToken))
        {
            throw new NullOrEmptyPluggyAccessTokenDomainException();
        }

        var items = await pluggyClient.GetItemsAsync(command.PluggyAccessToken, cancellationToken);
        var item = items.FirstOrDefault(candidate => string.Equals(candidate.Id, command.ItemId, StringComparison.Ordinal));
        if (item is null)
        {
            throw new PluggyApiCommunicationDomainException($"A instituição '{command.ItemId}' não foi encontrada na sessão atual da Pluggy.");
        }

        var accounts = await pluggyClient.GetAccountsByItemIdAsync(item.Id, command.PluggyAccessToken, cancellationToken);
        var checkingEvents = new List<TransactionIngested>();
        var cardEvents = new List<InvoiceItemIngested>();
        var sourceName = item.Connector.Name;
        var userId = command.UserId ?? string.Empty;

        foreach (var account in accounts)
        {
            var transactions = await pluggyClient.GetTransactionsByAccountIdAsync(account.Id, command.PluggyAccessToken, cancellationToken);

            foreach (var tx in transactions)
            {
                transactionMapper.MapTransactionToEvents(tx, account, sourceName, userId, checkingEvents, cardEvents);
            }
        }

        await PublishEventsAsync(checkingEvents, cardEvents, cancellationToken);

        return new SyncPluggySummaryDto(
            TotalItemsSynced: 1,
            TotalAccountsSynced: accounts.Count,
            TotalCheckingTransactionsIngested: checkingEvents.Count,
            TotalCardTransactionsIngested: cardEvents.Count,
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
