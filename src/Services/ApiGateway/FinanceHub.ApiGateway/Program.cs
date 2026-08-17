using System;
using System.Threading;

using DotNetEnv;

using FinanceHub.ApiGateway.Clients;
using FinanceHub.ApiGateway.Endpoints;
using FinanceHub.Shared.Observability;

namespace FinanceHub.ApiGateway;

public class Program
{
    public static async Task Main(string[] args)
    {
        Env.TraversePath().Load();

        var builder = WebApplication.CreateBuilder(args);

        builder.Host.UseFinanceHubSerilog();
        builder.Services.AddFinanceHubObservability(builder.Configuration, "FinanceHub.ApiGateway");

        builder.Services.AddApiGatewayServices(builder.Configuration);

        var app = builder.Build();

        app.UseExceptionHandler();

        app.UseAuthentication();
        app.UseAuthorization();
        app.UseRateLimiter();

        // 1. Health Endpoints
        app.MapGet("/health", () => Results.Ok(new
        {
            Status = GatewayConstants.Status.Healthy,
            Service = "FinanceHub.ApiGateway",
            Timestamp = DateTime.UtcNow,
            Version = "1.0.0-net10"
        })).WithName("GetHealth").AllowAnonymous();

        app.MapGet("/health/detailed", async (
            IAuthConsentServiceClient consentClient,
            ITransactionAggregatorServiceClient transactionClient,
            CancellationToken ct) =>
        {
            var authConsentHealthy = await consentClient.HealthCheckAsync(ct);
            var aggregatorHealthy = await transactionClient.HealthCheckAsync(ct);

            var isHealthy = authConsentHealthy && aggregatorHealthy;

            var result = new
            {
                Status = isHealthy ? GatewayConstants.Status.Healthy : GatewayConstants.Status.Degraded,
                Service = "FinanceHub.ApiGateway",
                Timestamp = DateTime.UtcNow,
                DownstreamServices = new
                {
                    AuthConsent = authConsentHealthy ? GatewayConstants.Status.Healthy : GatewayConstants.Status.Unhealthy,
                    TransactionAggregator = aggregatorHealthy ? GatewayConstants.Status.Healthy : GatewayConstants.Status.Unhealthy
                }
            };

            return isHealthy ? Results.Ok(result) : Results.Json(result, statusCode: StatusCodes.Status503ServiceUnavailable);
        }).WithName("GetDetailedHealth").AllowAnonymous();

        // 2. Gateway Endpoints
        app.MapAuthGatewayEndpoints();
        app.MapDashboardEndpoints();
        app.MapTransactionGatewayEndpoints();
        app.MapConsentGatewayEndpoints();
        app.MapPluggyGatewayEndpoints();

        await app.RunAsync();
    }
}
