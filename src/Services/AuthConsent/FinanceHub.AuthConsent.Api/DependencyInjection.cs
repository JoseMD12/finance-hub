using FinanceHub.AuthConsent.Api.Middleware;
using FinanceHub.AuthConsent.Application.Commands.AuthorizeConsent;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FinanceHub.AuthConsent.Api;

public static class DependencyInjection
{
    public static IServiceCollection AddAuthConsentApi(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<AuthorizeConsentCommandHandler>();

        services.AddExceptionHandler<GlobalExceptionHandler>();
        services.AddProblemDetails();

        return services;
    }
}
