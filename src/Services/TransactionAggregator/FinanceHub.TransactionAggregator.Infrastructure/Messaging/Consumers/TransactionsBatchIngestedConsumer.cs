using FinanceHub.Shared.Messaging.Events;
using FinanceHub.TransactionAggregator.Application.Commands.IngestTransaction;
using FinanceHub.TransactionAggregator.Domain.Entities;
using FinanceHub.TransactionAggregator.Domain.ValueObjects;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace FinanceHub.TransactionAggregator.Infrastructure.Messaging.Consumers;

public class TransactionsBatchIngestedConsumer : IConsumer<TransactionsBatchIngested>
{
    private readonly IIngestTransactionCommandHandler _handler;
    private readonly ILogger<TransactionsBatchIngestedConsumer> _logger;

    public TransactionsBatchIngestedConsumer(
        IIngestTransactionCommandHandler handler,
        ILogger<TransactionsBatchIngestedConsumer> logger)
    {
        _handler = handler;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<TransactionsBatchIngested> context)
    {
        var batch = context.Message;
        _logger.LogInformation(
            "Consumindo TransactionsBatchIngested [BatchId: {BatchId}, Chunk: {ChunkIndex}/{TotalChunks}, CheckingTxs: {CheckingCount}, CardTxs: {CardCount}]",
            batch.BatchId, batch.ChunkIndex, batch.TotalChunks, batch.CheckingTransactions.Count, batch.CardTransactions.Count);

        var userId = !string.IsNullOrWhiteSpace(batch.UserId) ? batch.UserId : "default-user";

        foreach (var checkingTx in batch.CheckingTransactions)
        {
            var type = checkingTx.Amount >= 0 ? TransactionType.Credit : TransactionType.Debit;
            var command = new IngestTransactionCommand(
                UserId: userId,
                InstitutionId: checkingTx.Source,
                AccountNumber: checkingTx.AccountId,
                BankTransactionId: checkingTx.BankTransactionId ?? Guid.NewGuid().ToString("N"),
                Amount: Math.Abs(checkingTx.Amount),
                Currency: checkingTx.Currency ?? "BRL",
                Type: type,
                RawDescription: checkingTx.Description,
                TransactionDateUtc: checkingTx.TransactionDate,
                Channel: TransactionChannel.Other,
                MerchantName: checkingTx.Description
            );

            await _handler.Handle(command, context.CancellationToken);
        }

        foreach (var cardTx in batch.CardTransactions)
        {
            var type = cardTx.Amount >= 0 ? TransactionType.Debit : TransactionType.Credit;
            var command = new IngestTransactionCommand(
                UserId: userId,
                InstitutionId: cardTx.Source,
                AccountNumber: cardTx.CreditCardAccountId,
                BankTransactionId: cardTx.BankTransactionId ?? Guid.NewGuid().ToString("N"),
                Amount: Math.Abs(cardTx.Amount),
                Currency: cardTx.Currency ?? "BRL",
                Type: type,
                RawDescription: cardTx.Description,
                TransactionDateUtc: cardTx.TransactionDate,
                Channel: TransactionChannel.CreditCard,
                MerchantName: cardTx.Description
            );

            await _handler.Handle(command, context.CancellationToken);
        }

        _logger.LogInformation("Lote TransactionsBatchIngested [BatchId: {BatchId}] processado com sucesso.", batch.BatchId);
    }
}
