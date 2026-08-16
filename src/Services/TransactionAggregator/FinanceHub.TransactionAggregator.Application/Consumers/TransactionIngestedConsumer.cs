using FinanceHub.Shared.Messaging.Events;
using FinanceHub.TransactionAggregator.Application.Commands.IngestTransaction;
using FinanceHub.TransactionAggregator.Domain.Entities;
using MassTransit;

namespace FinanceHub.TransactionAggregator.Application.Consumers;

public class TransactionIngestedConsumer : IConsumer<TransactionIngested>
{
    private readonly IIngestTransactionCommandHandler _commandHandler;

    public TransactionIngestedConsumer(IIngestTransactionCommandHandler commandHandler)
    {
        _commandHandler = commandHandler;
    }

    public async Task Consume(ConsumeContext<TransactionIngested> context)
    {
        var msg = context.Message;

        var amountMagnitude = Math.Abs(msg.Amount);
        var type = msg.Amount >= 0 ? TransactionType.Credit : TransactionType.Debit;

        var command = new IngestTransactionCommand(
            UserId: msg.UserId,
            InstitutionId: msg.Source,
            AccountNumber: msg.AccountId,
            BankTransactionId: msg.BankTransactionId ?? Guid.NewGuid().ToString(),
            Amount: amountMagnitude,
            Currency: msg.Currency,
            Type: type,
            RawDescription: msg.Description,
            TransactionDateUtc: msg.TransactionDate,
            Channel: TransactionChannel.Other,
            MerchantName: msg.Description
        );

        await _commandHandler.Handle(command, context.CancellationToken);
    }
}
