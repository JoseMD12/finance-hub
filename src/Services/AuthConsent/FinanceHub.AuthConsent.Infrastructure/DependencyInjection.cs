using FinanceHub.AuthConsent.Application.Interfaces;
using FinanceHub.AuthConsent.Domain.Constants;
using FinanceHub.AuthConsent.Infrastructure.BackgroundServices;
using FinanceHub.AuthConsent.Infrastructure.Persistence;
using FinanceHub.AuthConsent.Infrastructure.Persistence.Repositories;
using FinanceHub.AuthConsent.Infrastructure.Services;
using FinanceHub.AuthConsent.Infrastructure.Services.OAuthStrategies;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FinanceHub.AuthConsent.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddAuthConsentInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "A string de conexão 'ConnectionStrings:DefaultConnection' não foi informada no arquivo .env ou no ambiente.");
        }

        services.AddDbContext<AuthConsentDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddScoped<IBankConsentRepository, BankConsentRepository>();
        services.AddScoped<IEventPublisher, MassTransitEventPublisher>();

        // Estratégias OAuth2 (.NET 10 Keyed Services)
        services.AddKeyedScoped<IOAuthBankClientStrategy, ItauOAuthStrategy>(BankIdentifiers.Itau);
        services.AddKeyedScoped<IOAuthBankClientStrategy, MercadoPagoOAuthStrategy>(BankIdentifiers.MercadoPago);
        services.AddKeyedScoped<IOAuthBankClientStrategy, InterOAuthStrategy>(BankIdentifiers.Inter);
        services.AddScoped<IKeyedOAuthStrategyFactory, KeyedOAuthStrategyFactory>();

        // Worker proativo em segundo plano
        services.AddHostedService<TokenRenewalBackgroundService>();

        return services;
    }
}
