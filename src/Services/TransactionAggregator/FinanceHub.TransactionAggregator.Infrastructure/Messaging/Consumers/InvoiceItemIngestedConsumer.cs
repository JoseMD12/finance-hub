using FinanceHub.Shared.Messaging.Events;
using FinanceHub.TransactionAggregator.Application.Commands.IngestTransaction;
using FinanceHub.TransactionAggregator.Domain.Entities;
using FinanceHub.TransactionAggregator.Domain.ValueObjects;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace FinanceHub.TransactionAggregator.Infrastructure.Messaging.Consumers;

public class InvoiceItemIngestedConsumer : IConsumer<InvoiceItemIngested>
{
    private readonly IIngestTransactionCommandHandler _handler;
    private readonly ILogger<InvoiceItemIngestedConsumer> _logger;

    public InvoiceItemIngestedConsumer(
        IIngestTransactionCommandHandler handler,
        ILogger<InvoiceItemIngestedConsumer> logger)
    {
        _handler = handler;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<InvoiceItemIngested> context)
    {
        var msg = context.Message;
        _logger.LogInformation("Consumindo InvoiceItemIngested [Source: {Source}, CardAccount: {CardAccountId}, TxId: {TxId}]",
            msg.Source, msg.CreditCardAccountId, msg.BankTransactionId);

        var type = msg.Amount >= 0 ? TransactionType.Debit : TransactionType.Credit;

        var command = new IngestTransactionCommand(
            UserId: !string.IsNullOrWhiteSpace(msg.UserId) ? msg.UserId : "default-user",
            InstitutionId: msg.Source,
            AccountNumber: msg.CreditCardAccountId,
            BankTransactionId: msg.BankTransactionId ?? Guid.NewGuid().ToString("N"),
            Amount: Math.Abs(msg.Amount),
            Currency: msg.Currency ?? "BRL",
            Type: type,
            RawDescription: msg.Description,
            TransactionDateUtc: msg.TransactionDate,
            Channel: TransactionChannel.CreditCard,
            MerchantName: msg.Description
        );

        var id = await _handler.Handle(command, context.CancellationToken);
        _logger.LogInformation("InvoiceItemIngested persistida com sucesso [Id: {Id}]", id);
    }
}
