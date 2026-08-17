using FinanceHub.PluggyIntegration.Application.Commands.SyncAllPluggyAccounts;
using Microsoft.Extensions.DependencyInjection;

namespace FinanceHub.PluggyIntegration.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<ISyncAllPluggyAccountsCommandHandler, SyncAllPluggyAccountsCommandHandler>();
        return services;
    }
}
