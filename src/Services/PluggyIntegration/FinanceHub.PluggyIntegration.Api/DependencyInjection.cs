using FinanceHub.PluggyIntegration.Api.Middleware;
using FinanceHub.PluggyIntegration.Application;
using FinanceHub.PluggyIntegration.Infrastructure;
using FinanceHub.Shared.Messaging.Extensions;
using FinanceHub.Shared.Observability;
using FinanceHub.Shared.Observability.Exceptions.Mapping;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FinanceHub.PluggyIntegration.Api;

public static class DependencyInjection
{
    public static IServiceCollection AddPluggyIntegrationServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddApplicationServices();
        services.AddInfrastructureServices(configuration);
        services.AddFinanceHubMessaging(configuration);
        services.AddFinanceHubObservability(configuration, "FinanceHub.PluggyIntegration.Api");

        services.AddExceptionMappingServices();
        services.AddExceptionHandler<GlobalExceptionHandler>();
        services.AddProblemDetails();

        return services;
    }
}
