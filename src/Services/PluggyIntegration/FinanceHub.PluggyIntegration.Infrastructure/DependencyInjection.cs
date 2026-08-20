using System.Net;
using FinanceHub.PluggyIntegration.Application.Interfaces;
using FinanceHub.PluggyIntegration.Domain.Constants;
using FinanceHub.PluggyIntegration.Infrastructure.Clients;
using FinanceHub.PluggyIntegration.Infrastructure.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Polly;
using Polly.CircuitBreaker;

namespace FinanceHub.PluggyIntegration.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<PluggyOptions>(configuration.GetSection(PluggyOptions.SectionName));

        var baseUrl = configuration[PluggyConstants.EnvironmentVariables.ApiBaseUrl] 
                      ?? configuration[PluggyConstants.Configuration.ApiBaseUrlKey] 
                      ?? throw new InvalidOperationException($"A configuração '{PluggyConstants.EnvironmentVariables.ApiBaseUrl}' é obrigatória.");

        services.AddHttpClient<IPluggyHttpExecutor, PluggyHttpExecutor>(client =>
        {
            client.BaseAddress = new Uri(baseUrl);
            client.Timeout = TimeSpan.FromSeconds(PluggyConstants.Resilience.DefaultTimeoutSeconds);
        })
        .AddResilienceHandler("PluggyPipeline", builder =>
        {
            builder.AddTimeout(TimeSpan.FromSeconds(PluggyConstants.Resilience.PipelineTimeoutSeconds));
            builder.AddRetry(new HttpRetryStrategyOptions
            {
                MaxRetryAttempts = PluggyConstants.Resilience.MaxRetryAttempts,
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true,
                Delay = TimeSpan.FromMilliseconds(PluggyConstants.Resilience.BaseRetryDelayMilliseconds),
                ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                    .HandleResult(r => r.StatusCode == HttpStatusCode.TooManyRequests || (int)r.StatusCode >= (int)HttpStatusCode.InternalServerError)
            });
            builder.AddCircuitBreaker(new HttpCircuitBreakerStrategyOptions
            {
                SamplingDuration = TimeSpan.FromSeconds(PluggyConstants.Resilience.CircuitBreakerSamplingSeconds),
                MinimumThroughput = PluggyConstants.Resilience.CircuitBreakerMinimumThroughput,
                FailureRatio = PluggyConstants.Resilience.CircuitBreakerFailureRatio,
                BreakDuration = TimeSpan.FromSeconds(PluggyConstants.Resilience.CircuitBreakerBreakDurationSeconds),
                ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                    .HandleResult(r => (int)r.StatusCode >= (int)HttpStatusCode.InternalServerError)
            });
        });

        services.AddScoped<IMeuPluggyClient, MeuPluggyClient>();

        return services;
    }
}
