using FinanceHub.PluggyIntegration.Application.DTOs;
using FinanceHub.PluggyIntegration.Application.Interfaces;
using FinanceHub.PluggyIntegration.Domain.Constants;
using FinanceHub.Shared.Messaging.Events;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace FinanceHub.PluggyIntegration.Application.Commands.SyncAllPluggyAccounts;

public class SyncAllPluggyAccountsCommandHandler : ISyncAllPluggyAccountsCommandHandler
{
    private readonly IMeuPluggyClient _pluggyClient;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<SyncAllPluggyAccountsCommandHandler> _logger;

    public SyncAllPluggyAccountsCommandHandler(
        IMeuPluggyClient pluggyClient,
        IPublishEndpoint publishEndpoint,
        ILogger<SyncAllPluggyAccountsCommandHandler> logger)
    {
        _pluggyClient = pluggyClient;
        _publishEndpoint = publishEndpoint;
        _logger = logger;
    }

    public async Task<SyncPluggySummaryDto> HandleAsync(SyncAllPluggyAccountsCommand command, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Iniciando sincronização unificada de contas via Meu.Pluggy para UserId: {UserId}", command.UserId);

        var items = await _pluggyClient.GetItemsAsync(cancellationToken);
        _logger.LogInformation("Localizados {Count} bancos/itens conectados no Meu.Pluggy.", items.Count);

        int totalAccounts = 0;
        int totalCheckingTxs = 0;
        int totalCardTxs = 0;

        foreach (var item in items)
        {
            var accounts = await _pluggyClient.GetAccountsByItemIdAsync(item.Id, cancellationToken);
            totalAccounts += accounts.Count;

            foreach (var account in accounts)
            {
                var transactions = await _pluggyClient.GetTransactionsByAccountIdAsync(account.Id, cancellationToken);

                if (account.Type == PluggyConstants.AccountTypes.Credit || account.Subtype == PluggyConstants.AccountSubtypes.CreditCard)
                {
                    // Cartão de Crédito -> InvoiceItemIngested
                    DateTime? dueDate = null;
                    if (!string.IsNullOrWhiteSpace(account.CreditData?.BalanceDueDate) &&
                        DateTime.TryParse(account.CreditData.BalanceDueDate, out var parsedDueDate))
                    {
                        dueDate = DateTime.SpecifyKind(parsedDueDate, DateTimeKind.Utc);
                    }

                    foreach (var tx in transactions)
                    {
                        var txDate = DateTime.TryParse(tx.Date, out var dt) ? DateTime.SpecifyKind(dt, DateTimeKind.Utc) : DateTime.UtcNow;
                        var canonicalCategory = PluggyCategoryMapper.Map(tx.Category);

                        var cardEvent = new InvoiceItemIngested(
                            IngestionId: Guid.NewGuid(),
                            UserId: command.UserId,
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
                            Currency: account.CurrencyCode ?? "BRL",
                            RawPayloadJson: null,
                            OccurredAtUtc: DateTime.UtcNow
                        );

                        await _publishEndpoint.Publish(cardEvent, cancellationToken);
                        totalCardTxs++;
                    }
                }
                else
                {
                    // Conta Corrente -> TransactionIngested
                    foreach (var tx in transactions)
                    {
                        var txDate = DateTime.TryParse(tx.Date, out var dt) ? DateTime.SpecifyKind(dt, DateTimeKind.Utc) : DateTime.UtcNow;

                        var checkingEvent = new TransactionIngested(
                            IngestionId: Guid.NewGuid(),
                            UserId: command.UserId,
                            Source: item.Connector.Name,
                            AccountId: account.Id,
                            BankTransactionId: tx.Id,
                            Amount: tx.Amount,
                            TransactionDate: txDate,
                            Description: tx.Description,
                            Currency: account.CurrencyCode ?? "BRL",
                            RawPayloadJson: null,
                            OccurredAtUtc: DateTime.UtcNow
                        );

                        await _publishEndpoint.Publish(checkingEvent, cancellationToken);
                        totalCheckingTxs++;
                    }
                }
            }
        }

        _logger.LogInformation("Sincronização concluída: {Items} items, {Accounts} contas, {CheckingTxs} txs de conta corrente, {CardTxs} txs de cartão de crédito.",
            items.Count, totalAccounts, totalCheckingTxs, totalCardTxs);

        return new SyncPluggySummaryDto(
            TotalItemsSynced: items.Count,
            TotalAccountsSynced: totalAccounts,
            TotalCheckingTransactionsIngested: totalCheckingTxs,
            TotalCardTransactionsIngested: totalCardTxs,
            SyncedAtUtc: DateTime.UtcNow
        );
    }
}
