using System.Globalization;
using FinanceHub.PluggyIntegration.Application.DTOs;
using FinanceHub.PluggyIntegration.Application.Interfaces;
using FinanceHub.PluggyIntegration.Domain.Constants;
using FinanceHub.PluggyIntegration.Domain.Exceptions;
using FinanceHub.Shared.Messaging.Events;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace FinanceHub.PluggyIntegration.Application.Commands.SyncAllPluggyAccounts;

public sealed class SyncAllPluggyAccountsCommandHandler(
    IMeuPluggyClient pluggyClient,
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
        var accountsTask = pluggyClient.GetAllAccountsAsync(command.PluggyAccessToken, cancellationToken);

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

        var allTransactions = await pluggyClient.GetAllTransactionsAsync(command.PluggyAccessToken, cancellationToken);

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
            var txDate = ParseDate(tx.Date);

            if (IsCreditCardAccount(account))
            {
                DateTime? dueDate = ParseDueDate(account.CreditData?.BalanceDueDate);
                var canonicalCategory = PluggyCategoryMapper.Map(tx.Category);

                cardEvents.Add(new InvoiceItemIngested(
                    IngestionId: Guid.NewGuid(),
                    UserId: command.UserId,
                    Source: sourceName,
                    CreditCardAccountId: account.Id,
                    CardLastFourDigits: null,
                    BankTransactionId: tx.Id,
                    Amount: tx.Amount,
                    TransactionDate: txDate,
                    Description: tx.Description,
                    Category: canonicalCategory,
                    CurrentInstallment: null,
                    TotalInstallments: null,
                    InvoiceDueDate: dueDate,
                    Currency: account.CurrencyCode ?? PluggyConstants.DefaultCurrency,
                    RawPayloadJson: null,
                    OccurredAtUtc: DateTime.UtcNow
                ));
            }
            else
            {
                checkingEvents.Add(new TransactionIngested(
                    IngestionId: Guid.NewGuid(),
                    UserId: command.UserId,
                    Source: sourceName,
                    AccountId: account.Id,
                    BankTransactionId: tx.Id,
                    Amount: tx.Amount,
                    TransactionDate: txDate,
                    Description: tx.Description,
                    Currency: account.CurrencyCode ?? PluggyConstants.DefaultCurrency,
                    RawPayloadJson: null,
                    OccurredAtUtc: DateTime.UtcNow
                ));
            }
        }

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

    public async Task<SyncPluggySummaryDto> HandleItemAsync(SyncSinglePluggyItemCommand command, CancellationToken cancellationToken = default)
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

        foreach (var account in accounts)
        {
            var transactions = await pluggyClient.GetTransactionsByAccountIdAsync(account.Id, command.PluggyAccessToken, cancellationToken);

            foreach (var tx in transactions)
            {
                var txDate = ParseDate(tx.Date);

                if (IsCreditCardAccount(account))
                {
                    DateTime? dueDate = ParseDueDate(account.CreditData?.BalanceDueDate);
                    var canonicalCategory = PluggyCategoryMapper.Map(tx.Category);

                    cardEvents.Add(new InvoiceItemIngested(
                        IngestionId: Guid.NewGuid(),
                        UserId: command.UserId ?? string.Empty,
                        Source: sourceName,
                        CreditCardAccountId: account.Id,
                        CardLastFourDigits: null,
                        BankTransactionId: tx.Id,
                        Amount: tx.Amount,
                        TransactionDate: txDate,
                        Description: tx.Description,
                        Category: canonicalCategory,
                        CurrentInstallment: null,
                        TotalInstallments: null,
                        InvoiceDueDate: dueDate,
                        Currency: account.CurrencyCode ?? PluggyConstants.DefaultCurrency,
                        RawPayloadJson: null,
                        OccurredAtUtc: DateTime.UtcNow
                    ));
                }
                else
                {
                    checkingEvents.Add(new TransactionIngested(
                        IngestionId: Guid.NewGuid(),
                        UserId: command.UserId ?? string.Empty,
                        Source: sourceName,
                        AccountId: account.Id,
                        BankTransactionId: tx.Id,
                        Amount: tx.Amount,
                        TransactionDate: txDate,
                        Description: tx.Description,
                        Currency: account.CurrencyCode ?? PluggyConstants.DefaultCurrency,
                        RawPayloadJson: null,
                        OccurredAtUtc: DateTime.UtcNow
                    ));
                }
            }
        }

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

        return new SyncPluggySummaryDto(
            TotalItemsSynced: 1,
            TotalAccountsSynced: accounts.Count,
            TotalCheckingTransactionsIngested: checkingEvents.Count,
            TotalCardTransactionsIngested: cardEvents.Count,
            SyncedAtUtc: DateTime.UtcNow
        );
    }
    private static bool IsCreditCardAccount(PluggyAccountDto account) =>
        account.Type == PluggyConstants.AccountTypes.Credit ||
        account.Subtype == PluggyConstants.AccountSubtypes.CreditCard;

    private static DateTime? ParseDueDate(string? rawDueDate)
    {
        if (!string.IsNullOrWhiteSpace(rawDueDate) &&
            DateTime.TryParse(rawDueDate, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var parsedDueDate))
        {
            return DateTime.SpecifyKind(parsedDueDate, DateTimeKind.Utc);
        }

        return null;
    }

    private static DateTime ParseDate(string? rawDate)
    {
        return DateTime.TryParse(rawDate, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var dt)
            ? DateTime.SpecifyKind(dt, DateTimeKind.Utc)
            : DateTime.UtcNow;
    }
}
