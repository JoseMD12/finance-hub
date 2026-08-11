using FinanceHub.Shared.Observability;

namespace FinanceHub.TransactionAggregator.Api;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Host.UseFinanceHubSerilog();
        builder.Services.AddFinanceHubObservability(builder.Configuration, "FinanceHub.TransactionAggregator.Api");

        var app = builder.Build();

        app.UseHttpsRedirection();

        app.MapGet("/health", () => Results.Ok(new
        {
            Status = "Healthy",
            Service = "FinanceHub.TransactionAggregator.Api",
            Timestamp = DateTime.UtcNow,
            Version = "1.0.0-net10"
        })).WithName("GetHealth");

        app.Run();
    }
}
