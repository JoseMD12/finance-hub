using Microsoft.Extensions.DependencyInjection;

namespace FinanceHub.Shared.Observability.Exceptions.Mapping;

public static class ExceptionMappingExtensions
{
    public static IServiceCollection AddExceptionMappingServices(this IServiceCollection services)
    {
        services.AddSingleton<IExceptionMapper, InfrastructureExceptionMapper>();
        services.AddSingleton<IExceptionMapper, DomainExceptionMapper>();
        services.AddSingleton<IExceptionMapper, DefaultExceptionMapper>();
        services.AddSingleton<IExceptionMapperRegistry, ExceptionMapperRegistry>();

        return services;
    }
}
