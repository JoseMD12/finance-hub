using FinanceHub.MercadoPagoIntegration.Application.Commands.CreateConnectToken;
using FinanceHub.MercadoPagoIntegration.Application.Commands.SyncTransactions;
using Microsoft.Extensions.DependencyInjection;

namespace FinanceHub.MercadoPagoIntegration.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddMercadoPagoApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<ICreateConnectTokenCommandHandler, CreateConnectTokenCommandHandler>();
        services.AddScoped<ISyncMercadoPagoOpenFinanceCommandHandler, SyncMercadoPagoOpenFinanceCommandHandler>();
        services.AddSingleton(TimeProvider.System);

        return services;
    }
}
