using FinanceHub.TransactionAggregator.Api.Middleware;
using FinanceHub.TransactionAggregator.Application;
using FinanceHub.TransactionAggregator.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FinanceHub.TransactionAggregator.Api;

public static class DependencyInjection
{
    public static IServiceCollection AddTransactionAggregatorApiServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSingleton(TimeProvider.System);

        services.AddTransactionAggregatorApplicationServices();
        services.AddTransactionAggregatorInfrastructureServices(configuration);

        services.AddExceptionHandler<GlobalExceptionHandler>();
        services.AddProblemDetails();

        return services;
    }
}
