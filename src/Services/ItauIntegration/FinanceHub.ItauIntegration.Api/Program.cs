using FinanceHub.Shared.Observability;

namespace FinanceHub.ItauIntegration.Api;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Host.UseFinanceHubSerilog();
        builder.Services.AddFinanceHubObservability(builder.Configuration, "FinanceHub.ItauIntegration.Api");

        var app = builder.Build();

        app.UseHttpsRedirection();

        app.MapGet("/health", () => Results.Ok(new
        {
            Status = "Healthy",
            Service = "FinanceHub.ItauIntegration.Api",
            Timestamp = DateTime.UtcNow,
            Version = "1.0.0-net10"
        })).WithName("GetHealth");

        app.Run();
    }
}
