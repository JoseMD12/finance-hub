using FinanceHub.Shared.Messaging.Events;
using FinanceHub.TransactionAggregator.Application.Commands.IngestTransaction;
using FinanceHub.TransactionAggregator.Domain.Entities;
using FinanceHub.TransactionAggregator.Domain.ValueObjects;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace FinanceHub.TransactionAggregator.Infrastructure.Messaging.Consumers;

public class TransactionIngestedConsumer : IConsumer<TransactionIngested>
{
    private readonly IIngestTransactionCommandHandler _handler;
    private readonly ILogger<TransactionIngestedConsumer> _logger;

    public TransactionIngestedConsumer(
        IIngestTransactionCommandHandler handler,
        ILogger<TransactionIngestedConsumer> logger)
    {
        _handler = handler;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<TransactionIngested> context)
    {
        var msg = context.Message;
        _logger.LogInformation("Consumindo TransactionIngested [Source: {Source}, AccountId: {AccountId}, BankTxId: {TxId}]",
            msg.Source, msg.AccountId, msg.BankTransactionId);

        var type = msg.Amount >= 0 ? TransactionType.Credit : TransactionType.Debit;

        var command = new IngestTransactionCommand(
            UserId: !string.IsNullOrWhiteSpace(msg.UserId) ? msg.UserId : "default-user",
            InstitutionId: msg.Source,
            AccountNumber: msg.AccountId,
            BankTransactionId: msg.BankTransactionId ?? Guid.NewGuid().ToString("N"),
            Amount: Math.Abs(msg.Amount),
            Currency: msg.Currency ?? "BRL",
            Type: type,
            RawDescription: msg.Description,
            TransactionDateUtc: msg.TransactionDate,
            Channel: TransactionChannel.Other,
            MerchantName: msg.Description
        );

        var id = await _handler.Handle(command, context.CancellationToken);
        _logger.LogInformation("TransactionIngested persistida com sucesso [Id: {Id}]", id);
    }
}
