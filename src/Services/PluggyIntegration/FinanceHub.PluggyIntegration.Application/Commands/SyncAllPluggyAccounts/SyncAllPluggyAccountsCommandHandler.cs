using System.Globalization;
using FinanceHub.PluggyIntegration.Application.DTOs;
using FinanceHub.PluggyIntegration.Application.Interfaces;
using FinanceHub.PluggyIntegration.Domain.Constants;
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
        logger.LogInformation("Iniciando sincronização unificada de contas via Meu.Pluggy para UserId: {UserId}", command.UserId);

        var items = await pluggyClient.GetItemsAsync(cancellationToken);

        int totalAccounts = 0;
        int totalCheckingTxs = 0;
        int totalCardTxs = 0;

        foreach (var item in items)
        {
            var accounts = await pluggyClient.GetAccountsByItemIdAsync(item.Id, cancellationToken);
            totalAccounts += accounts.Count;

            foreach (var account in accounts)
            {
                var (checkingCount, cardCount) = await ProcessAccountAsync(item, account, command.UserId, cancellationToken);
                totalCheckingTxs += checkingCount;
                totalCardTxs += cardCount;
            }
        }

        logger.LogInformation("Sincronização concluída: {Items} items, {Accounts} contas, {CheckingTxs} txs de conta corrente, {CardTxs} txs de cartão de crédito.",
            items.Count, totalAccounts, totalCheckingTxs, totalCardTxs);

        return new SyncPluggySummaryDto(
            TotalItemsSynced: items.Count,
            TotalAccountsSynced: totalAccounts,
            TotalCheckingTransactionsIngested: totalCheckingTxs,
            TotalCardTransactionsIngested: totalCardTxs,
            SyncedAtUtc: DateTime.UtcNow
        );
    }

    private async Task<(int checkingCount, int cardCount)> ProcessAccountAsync(
        PluggyItemDto item,
        PluggyAccountDto account,
        string? userId,
        CancellationToken cancellationToken)
    {
        var transactions = await pluggyClient.GetTransactionsByAccountIdAsync(account.Id, cancellationToken);

        if (IsCreditCardAccount(account))
        {
            int cardCount = await ProcessCreditCardTransactionsAsync(item, account, transactions, userId, cancellationToken);
            return (0, cardCount);
        }

        int checkingCount = await ProcessCheckingTransactionsAsync(item, account, transactions, userId, cancellationToken);
        return (checkingCount, 0);
    }

    private static bool IsCreditCardAccount(PluggyAccountDto account) =>
        account.Type == PluggyConstants.AccountTypes.Credit ||
        account.Subtype == PluggyConstants.AccountSubtypes.CreditCard;

    private async Task<int> ProcessCreditCardTransactionsAsync(
        PluggyItemDto item,
        PluggyAccountDto account,
        IReadOnlyList<PluggyTransactionDto> transactions,
        string? userId,
        CancellationToken cancellationToken)
    {
        DateTime? dueDate = ParseDueDate(account.CreditData?.BalanceDueDate);
        int count = 0;

        foreach (var tx in transactions)
        {
            var txDate = ParseDate(tx.Date);
            var canonicalCategory = PluggyCategoryMapper.Map(tx.Category);

            var cardEvent = new InvoiceItemIngested(
                IngestionId: Guid.NewGuid(),
                UserId: userId,
                Source: item.Connector.Name,
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
            );

            await publishEndpoint.Publish(cardEvent, cancellationToken);
            count++;
        }

        return count;
    }

    private async Task<int> ProcessCheckingTransactionsAsync(
        PluggyItemDto item,
        PluggyAccountDto account,
        IReadOnlyList<PluggyTransactionDto> transactions,
        string? userId,
        CancellationToken cancellationToken)
    {
        int count = 0;

        foreach (var tx in transactions)
        {
            var txDate = ParseDate(tx.Date);

            var checkingEvent = new TransactionIngested(
                IngestionId: Guid.NewGuid(),
                UserId: userId,
                Source: item.Connector.Name,
                AccountId: account.Id,
                BankTransactionId: tx.Id,
                Amount: tx.Amount,
                TransactionDate: txDate,
                Description: tx.Description,
                Currency: account.CurrencyCode ?? PluggyConstants.DefaultCurrency,
                RawPayloadJson: null,
                OccurredAtUtc: DateTime.UtcNow
            );

            await publishEndpoint.Publish(checkingEvent, cancellationToken);
            count++;
        }

        return count;
    }

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
