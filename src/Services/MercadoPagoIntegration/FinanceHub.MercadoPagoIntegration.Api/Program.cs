using DotNetEnv;
using FinanceHub.MercadoPagoIntegration.Api.Endpoints;
using FinanceHub.MercadoPagoIntegration.Infrastructure;
using FinanceHub.MercadoPagoIntegration.Infrastructure.Persistence;
using FinanceHub.Shared.Observability;

namespace FinanceHub.MercadoPagoIntegration.Api;

public class Program
{
    public static void Main(string[] args)
    {
        Env.TraversePath().Load();

        var builder = WebApplication.CreateBuilder(args);

        builder.Host.UseFinanceHubSerilog();
        builder.Services.AddFinanceHubObservability(builder.Configuration, "FinanceHub.MercadoPagoIntegration.Api");

        builder.Services.AddMercadoPagoInfrastructureServices(builder.Configuration);
        builder.Services.AddMercadoPagoApiServices(builder.Configuration);

        var app = builder.Build();

        try
        {
            using var scope = app.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetService<MercadoPagoDbContext>();
            dbContext?.Database.EnsureCreated();
        }
        catch
        {
            // Ignored during testing or when DB migrations are handled externally
        }

        app.UseExceptionHandler();
        app.UseHttpsRedirection();

        app.MapGet("/health", () => Results.Ok(new
        {
            Status = "Healthy",
            Service = "FinanceHub.MercadoPagoIntegration.Api",
            Timestamp = DateTime.UtcNow,
            Version = "1.0.0-net10"
        })).WithName("GetHealth");

        app.MapSyncEndpoints();

        app.Run();
    }
}
