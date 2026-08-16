using FinanceHub.MercadoPagoIntegration.Api.Middleware;
using FinanceHub.MercadoPagoIntegration.Application;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FinanceHub.MercadoPagoIntegration.Api;

public static class DependencyInjection
{
    public static IServiceCollection AddMercadoPagoApiServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSingleton(TimeProvider.System);

        // Application Services
        services.AddMercadoPagoApplicationServices();

        // Exception Handling RFC 7807
        services.AddExceptionHandler<GlobalExceptionHandler>();
        services.AddProblemDetails();

        return services;
    }
}
