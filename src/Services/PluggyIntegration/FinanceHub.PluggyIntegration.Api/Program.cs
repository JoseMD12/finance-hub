using DotNetEnv;
using FinanceHub.PluggyIntegration.Api.Endpoints;
using FinanceHub.Shared.Observability;

namespace FinanceHub.PluggyIntegration.Api;

public class Program
{
    public static void Main(string[] args)
    {
        Env.TraversePath().Load();

        var builder = WebApplication.CreateBuilder(args);

        builder.Host.UseFinanceHubSerilog();
        builder.Services.AddPluggyIntegrationServices(builder.Configuration);

        var app = builder.Build();

        app.UseExceptionHandler();
        app.UseHttpsRedirection();

        app.MapGet("/health", () => Results.Ok(new
        {
            Status = "Healthy",
            Service = "FinanceHub.PluggyIntegration.Api",
            Timestamp = DateTime.UtcNow,
            Version = "1.0.0-net10"
        })).WithName("GetHealth");

        app.MapPluggyEndpoints();

        app.Run();
    }
}
