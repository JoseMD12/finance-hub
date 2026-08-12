using FinanceHub.AuthConsent.Api.Middleware;
using FinanceHub.AuthConsent.Application.Commands.AuthorizeConsent;
using FinanceHub.AuthConsent.Application.Commands.CreateConsent;
using FinanceHub.AuthConsent.Application.Commands.RenewToken;
using FinanceHub.AuthConsent.Application.Commands.RevokeConsent;
using FinanceHub.AuthConsent.Application.Queries.GetConsentByUserId;
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

        // Application Command & Query Handlers (Inversao de Dependencia com Interfaces)
        services.AddScoped<ICreateConsentCommandHandler, CreateConsentCommandHandler>();
        services.AddScoped<IAuthorizeConsentCommandHandler, AuthorizeConsentCommandHandler>();
        services.AddScoped<IRenewTokenCommandHandler, RenewTokenCommandHandler>();
        services.AddScoped<IRevokeConsentCommandHandler, RevokeConsentCommandHandler>();
        services.AddScoped<IGetConsentByUserIdQueryHandler, GetConsentByUserIdQueryHandler>();

        // Exception Handling RFC 7807
        services.AddExceptionHandler<GlobalExceptionHandler>();
        services.AddProblemDetails();

        return services;
    }
}
