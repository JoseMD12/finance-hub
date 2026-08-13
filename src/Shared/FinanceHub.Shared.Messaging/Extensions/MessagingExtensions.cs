using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FinanceHub.Shared.Messaging.Extensions;

public static class MessagingExtensions
{
    public static IServiceCollection AddFinanceHubMessaging(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<IBusRegistrationConfigurator>? configureConsumers = null)
    {
        var host = configuration["RabbitMQ:Host"]
            ?? throw new InvalidOperationException("Required environment variable 'RabbitMQ:Host' is not configured.");

        var portRaw = configuration["RabbitMQ:Port"]
            ?? throw new InvalidOperationException("Required environment variable 'RabbitMQ:Port' is not configured.");

        if (!ushort.TryParse(portRaw, out var port))
            throw new InvalidOperationException($"'RabbitMQ:Port' value '{portRaw}' is not a valid port number.");

        var username = configuration["RabbitMQ:Username"]
            ?? throw new InvalidOperationException("Required environment variable 'RabbitMQ:Username' is not configured.");

        var password = configuration["RabbitMQ:Password"]
            ?? throw new InvalidOperationException("Required environment variable 'RabbitMQ:Password' is not configured.");

        services.AddMassTransit(x =>
        {
            x.SetKebabCaseEndpointNameFormatter();

            configureConsumers?.Invoke(x);

            x.UsingRabbitMq((context, cfg) =>
            {
                cfg.Host(host, port, "/", h =>
                {
                    h.Username(username);
                    h.Password(password);
                });

                cfg.ConfigureEndpoints(context);
            });
        });

        return services;
    }
}
