using FinanceHub.Shared.Messaging.Events;
using FinanceHub.TransactionAggregator.Domain.Entities;
using FinanceHub.TransactionAggregator.Infrastructure.Persistence;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FinanceHub.TransactionAggregator.Infrastructure.Messaging.Filters;

public class IdempotentConsumerFilter<T> : IFilter<ConsumeContext<T>> where T : class, IIdempotentEvent
{
    private readonly TransactionAggregatorDbContext _dbContext;
    private readonly ILogger<IdempotentConsumerFilter<T>> _logger;

    public IdempotentConsumerFilter(TransactionAggregatorDbContext dbContext, ILogger<IdempotentConsumerFilter<T>> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public void Probe(ProbeContext context)
    {
        context.CreateFilterScope("idempotentConsumer");
    }

    public async Task Send(ConsumeContext<T> context, IPipe<ConsumeContext<T>> next)
    {
        var messageHash = context.Message.MessageHash;
        if (string.IsNullOrWhiteSpace(messageHash))
        {
            await next.Send(context);
            return;
        }

        var alreadyProcessed = await _dbContext.InboxProcessedMessages
            .AnyAsync(m => m.MessageHash == messageHash, context.CancellationToken);

        if (alreadyProcessed)
        {
            _logger.LogInformation("Mensagem duplicada detectada no Inbox (Hash: {MessageHash}). Ignorando reprocessamento.", messageHash);
            return;
        }

        await next.Send(context);

        _dbContext.InboxProcessedMessages.Add(new InboxProcessedMessage(messageHash, typeof(T).Name));
        try
        {
            await _dbContext.SaveChangesAsync(context.CancellationToken);
        }
        catch (DbUpdateException)
        {
            // Ignora violação de chave concorrente
        }
    }
}
