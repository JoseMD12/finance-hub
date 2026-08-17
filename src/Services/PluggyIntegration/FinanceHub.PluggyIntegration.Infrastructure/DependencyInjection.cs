using System.Net;
using FinanceHub.PluggyIntegration.Application.Interfaces;
using FinanceHub.PluggyIntegration.Domain.Constants;
using FinanceHub.PluggyIntegration.Infrastructure.Clients;
using FinanceHub.PluggyIntegration.Infrastructure.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.Extensions.Http;

namespace FinanceHub.PluggyIntegration.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<PluggyOptions>(configuration.GetSection(PluggyOptions.SectionName));

        var baseUrl = configuration[PluggyConstants.EnvironmentVariables.ApiBaseUrl] 
                      ?? configuration[PluggyConstants.Configuration.ApiBaseUrlKey] 
                      ?? PluggyConstants.DefaultBaseUrl;

        services.AddHttpClient<IMeuPluggyClient, MeuPluggyClient>(client =>
        {
            client.BaseAddress = new Uri(baseUrl);
            client.Timeout = TimeSpan.FromSeconds(PluggyConstants.Resilience.DefaultTimeoutSeconds);
        })
        .AddPolicyHandler(GetRetryPolicy());

        return services;
    }

    private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .OrResult(msg => msg.StatusCode == HttpStatusCode.TooManyRequests)
            .WaitAndRetryAsync(
                PluggyConstants.Resilience.MaxRetryAttempts,
                retryAttempt => TimeSpan.FromMilliseconds(
                    PluggyConstants.Resilience.BaseRetryDelayMilliseconds * Math.Pow(2, retryAttempt) 
                    + Random.Shared.Next(10, 100) // Jitter
                )
            );
    }
}
