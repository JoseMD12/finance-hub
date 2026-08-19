using FinanceHub.PluggyIntegration.Application.Commands.SyncAllPluggyAccounts;
using FinanceHub.PluggyIntegration.Application.Queries.GetPluggyItems;
using FinanceHub.PluggyIntegration.Application.Queries.GetPluggyAccounts;
using Microsoft.Extensions.DependencyInjection;

namespace FinanceHub.PluggyIntegration.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<ISyncAllPluggyAccountsCommandHandler, SyncAllPluggyAccountsCommandHandler>();
        services.AddScoped<IGetPluggyItemsQueryHandler, GetPluggyItemsQueryHandler>();
        services.AddScoped<IGetPluggyAccountsQueryHandler, GetPluggyAccountsQueryHandler>();
        return services;
    }
}
