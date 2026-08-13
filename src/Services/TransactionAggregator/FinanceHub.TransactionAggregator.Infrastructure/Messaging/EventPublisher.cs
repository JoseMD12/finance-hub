using System.Threading;
using System.Threading.Tasks;
using FinanceHub.TransactionAggregator.Application.Interfaces;
using MassTransit;

namespace FinanceHub.TransactionAggregator.Infrastructure.Messaging;

/// <summary>
/// Infrastructure implementation of IEventPublisher using MassTransit IBus.
/// Events are routed through the Transactional Outbox configured in EF Core,
/// guaranteeing at-least-once delivery without dual-write risk.
/// </summary>
public class EventPublisher(IBus bus) : IEventPublisher
{
    public Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken)
        where TEvent : class
        => bus.Publish(@event, cancellationToken);
}
