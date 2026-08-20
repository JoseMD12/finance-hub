using FinanceHub.Shared.Messaging.Extensions;
using FinanceHub.TransactionAggregator.Application.Interfaces;
using FinanceHub.TransactionAggregator.Infrastructure.Messaging;
using FinanceHub.TransactionAggregator.Infrastructure.Persistence;
using FinanceHub.TransactionAggregator.Infrastructure.Persistence.Repositories;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FinanceHub.TransactionAggregator.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddTransactionAggregatorInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // ── Persistence ─────────────────────────────────────────────────────────
        var connectionString = configuration.GetConnectionString("TransactionAggregatorDb")
                            ?? configuration.GetConnectionString("DefaultConnection");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "A string de conexão 'ConnectionStrings:TransactionAggregatorDb' (ou 'ConnectionStrings:DefaultConnection') não foi informada no ambiente.");
        }

        services.AddDbContext<TransactionAggregatorDbContext>(options =>
        {
            options.UseNpgsql(connectionString);
        });

        services.AddScoped<ITransactionRepository, TransactionRepository>();
        services.AddScoped<IAccountBalanceRepository, AccountBalanceRepository>();
        services.AddScoped<IUserCategoryRuleRepository, UserCategoryRuleRepository>();

        // ── Messaging — MassTransit + Transactional Outbox ──────────────────────
        services.AddFinanceHubMessaging(configuration, busConfig =>
        {
            busConfig.AddConsumer<Messaging.Consumers.TransactionIngestedConsumer>();
            busConfig.AddConsumer<Messaging.Consumers.InvoiceItemIngestedConsumer>();
            busConfig.AddConsumer<Messaging.Consumers.TransactionsBatchIngestedConsumer>();

            busConfig.AddEntityFrameworkOutbox<TransactionAggregatorDbContext>(outbox =>
            {
                outbox.UsePostgres();
                outbox.UseBusOutbox();
            });
        });

        services.AddScoped<IEventPublisher, EventPublisher>();

        return services;
    }
}
