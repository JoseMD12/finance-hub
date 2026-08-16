using FinanceHub.MercadoPagoIntegration.Application.Interfaces;
using FinanceHub.MercadoPagoIntegration.Infrastructure.Configuration;
using FinanceHub.MercadoPagoIntegration.Infrastructure.Persistence;
using FinanceHub.MercadoPagoIntegration.Infrastructure.Persistence.Repositories;
using FinanceHub.MercadoPagoIntegration.Infrastructure.Services;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FinanceHub.MercadoPagoIntegration.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddMercadoPagoInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // 1. Configuration
        services.Configure<OpenFinanceOptions>(configuration.GetSection(OpenFinanceOptions.SectionName));
        var openFinanceBaseUrl = configuration["OpenFinance:BaseUrl"] ?? "https://api.pluggy.ai";

        // 2. Open Finance Client with Resilience
        services.AddHttpClient<IOpenFinanceClient, PluggyOpenFinanceClient>(client =>
        {
            client.BaseAddress = new Uri(openFinanceBaseUrl);
            client.Timeout = TimeSpan.FromSeconds(30);
        })
        .AddStandardResilienceHandler();

        // 3. Database Context & Repositories
        var connectionString = configuration.GetConnectionString("MercadoPagoIntegrationDb")
                            ?? configuration.GetConnectionString("DefaultConnection")
                            ?? "Host=localhost;Port=5432;Database=financehub_mercadopago;Username=financehub;Password=financehub_password";

        services.AddDbContext<MercadoPagoDbContext>(options =>
        {
            options.UseNpgsql(connectionString, npgsqlOptions =>
            {
                npgsqlOptions.MigrationsAssembly(typeof(MercadoPagoDbContext).Assembly.FullName);
                npgsqlOptions.EnableRetryOnFailure(maxRetryCount: 3);
            });
        });

        services.AddScoped<IMercadoPagoSyncStateRepository, MercadoPagoSyncStateRepository>();

        // 4. Messaging & Transactional Outbox
        var rabbitHost = configuration["RabbitMQ:Host"] ?? "localhost";
        var rabbitPort = ushort.TryParse(configuration["RabbitMQ:Port"], out var port) ? port : (ushort)5672;
        var rabbitUser = configuration["RabbitMQ:Username"] ?? "guest";
        var rabbitPass = configuration["RabbitMQ:Password"] ?? "guest";

        services.AddMassTransit(x =>
        {
            x.AddEntityFrameworkOutbox<MercadoPagoDbContext>(o =>
            {
                o.UsePostgres();
                o.UseBusOutbox();
            });

            x.UsingRabbitMq((ctx, cfg) =>
            {
                cfg.Host(rabbitHost, rabbitPort, "/", h =>
                {
                    h.Username(rabbitUser);
                    h.Password(rabbitPass);
                });

                cfg.ConfigureEndpoints(ctx);
            });
        });

        return services;
    }
}
