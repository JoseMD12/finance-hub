using FinanceHub.PluggyIntegration.Application.DTOs;
using FinanceHub.PluggyIntegration.Application.Interfaces;
using FinanceHub.PluggyIntegration.Application.Services;
using FinanceHub.PluggyIntegration.Domain.Constants;
using FinanceHub.PluggyIntegration.Domain.Entities;
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
    private const int ChunkSize = 50;

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

        var balanceItems = accounts.Select(acc =>
        {
            var item = itemMap.GetValueOrDefault(acc.ItemId);
            var institutionName = item?.Connector.Name ?? acc.Name;
            var isCard = string.Equals(acc.Type, PluggyConstants.AccountTypes.Credit, StringComparison.OrdinalIgnoreCase)
                      || string.Equals(acc.Subtype, PluggyConstants.AccountSubtypes.CreditCard, StringComparison.OrdinalIgnoreCase);
            var signedBalance = isCard ? -Math.Abs(acc.Balance) : acc.Balance;

            return new AccountBalanceSnapshotItem(
                AccountId: acc.Id,
                InstitutionId: institutionName,
                AccountType: acc.Type,
                CurrentBalance: signedBalance,
                Currency: "BRL",
                SnapshotAtUtc: DateTime.UtcNow,
                IsCreditCard: isCard,
                CreditLimit: acc.CreditData?.CreditLimit,
                AvailableCreditLimit: acc.CreditData?.AvailableCreditLimit,
                // Gravado como a Meu.Pluggy devolve. Atenção: é o vencimento da ÚLTIMA fatura
                // fechada, frequentemente no passado — rolar para frente é responsabilidade do
                // ciclo, não da ingestão (ver seção 2.2 da spec do Dashboard).
                InvoiceDueDateUtc: PluggyAccount.ParseUtcDate(acc.CreditData?.BalanceDueDate),
                InvoiceClosingDateUtc: PluggyAccount.ParseUtcDate(acc.CreditData?.BalanceCloseDate));
        }).ToList();

        LogCreditDataDiscovery(accounts);

        await publishEndpoint.Publish(new AccountBalanceSnapshotSynchronized(
            command.UserId,
            balanceItems,
            DateTime.UtcNow), cancellationToken);

        await PublishBatchEventsAsync(command.UserId, checkingEvents, cardEvents, cancellationToken);

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

    /// <summary>
    /// Registra o que <c>creditData</c> realmente traz por connector no plano gratuito da
    /// Meu.Pluggy: quais campos vieram preenchidos e quais chegaram sem mapeamento nosso.
    /// Existe porque o contrato do plano gratuito varia por instituição e não deve ser
    /// presumido — a origem do dia de fechamento depende dessa observação.
    ///
    /// Loga apenas nomes de campo e valores de limite/data. Nada de nome de titular, número de
    /// conta ou qualquer PII, conforme a política de redação de logs (LGPD).
    /// </summary>
    private void LogCreditDataDiscovery(IReadOnlyList<PluggyAccountDto> accounts)
    {
        foreach (var account in accounts)
        {
            if (account.CreditData is null)
            {
                continue;
            }

            var unmappedFields = account.CreditData.AdditionalFields is { Count: > 0 }
                ? string.Join(", ", account.CreditData.AdditionalFields.Keys)
                : "(nenhum)";

            logger.LogInformation(
                "Descoberta creditData [Type: {Type}/{Subtype}] HasDueDate={HasDueDate} HasCloseDate={HasCloseDate} HasCreditLimit={HasCreditLimit} HasAvailableLimit={HasAvailableLimit} CamposNaoMapeados=[{UnmappedFields}]",
                account.Type,
                account.Subtype,
                !string.IsNullOrWhiteSpace(account.CreditData.BalanceDueDate),
                !string.IsNullOrWhiteSpace(account.CreditData.BalanceCloseDate),
                account.CreditData.CreditLimit.HasValue,
                account.CreditData.AvailableCreditLimit.HasValue,
                unmappedFields);
        }
    }

    private async Task PublishBatchEventsAsync(
        string userId,
        IReadOnlyList<TransactionIngested> checkingEvents,
        IReadOnlyList<InvoiceItemIngested> cardEvents,
        CancellationToken cancellationToken)
    {
        if (checkingEvents.Count == 0 && cardEvents.Count == 0)
        {
            return;
        }

        var batchId = Guid.NewGuid();
        var checkingChunks = checkingEvents.Chunk(ChunkSize).ToList();
        var cardChunks = cardEvents.Chunk(ChunkSize).ToList();
        int totalChunks = Math.Max(checkingChunks.Count, cardChunks.Count);
        if (totalChunks == 0) totalChunks = 1;

        for (int i = 0; i < totalChunks; i++)
        {
            var checkingChunk = i < checkingChunks.Count ? checkingChunks[i] : Array.Empty<TransactionIngested>();
            var cardChunk = i < cardChunks.Count ? cardChunks[i] : Array.Empty<InvoiceItemIngested>();

            var batchEvent = new TransactionsBatchIngested(
                BatchId: batchId,
                UserId: userId,
                ChunkIndex: i + 1,
                TotalChunks: totalChunks,
                CheckingTransactions: checkingChunk,
                CardTransactions: cardChunk,
                OccurredAtUtc: DateTime.UtcNow
            );

            await publishEndpoint.Publish(batchEvent, cancellationToken);
        }
    }
}
