using FinanceHub.AuthConsent.Application.Interfaces;
using MassTransit;

namespace FinanceHub.AuthConsent.Infrastructure.Services;

public sealed class MassTransitEventPublisher(IPublishEndpoint publishEndpoint) : IEventPublisher
{
    public async Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default) where TEvent : class
    {
        await publishEndpoint.Publish(@event, cancellationToken);
    }
}
