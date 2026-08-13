using System.Threading;
using System.Threading.Tasks;

namespace FinanceHub.TransactionAggregator.Application.Interfaces;

/// <summary>
/// Abstraction for publishing domain events from the Application layer.
/// The infrastructure implementation uses MassTransit IBus with Transactional Outbox.
/// </summary>
public interface IEventPublisher
{
    Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken)
        where TEvent : class;
}
