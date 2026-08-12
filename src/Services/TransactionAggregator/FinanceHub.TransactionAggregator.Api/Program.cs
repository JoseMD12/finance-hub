using DotNetEnv;
using FinanceHub.Shared.Observability;
using FinanceHub.TransactionAggregator.Api.Endpoints;

namespace FinanceHub.TransactionAggregator.Api;

public class Program
{
    public static void Main(string[] args)
    {
        Env.TraversePath().Load();

        var builder = WebApplication.CreateBuilder(args);

        builder.Host.UseFinanceHubSerilog();
        builder.Services.AddFinanceHubObservability(builder.Configuration, "FinanceHub.TransactionAggregator.Api");
        builder.Services.AddTransactionAggregatorApiServices(builder.Configuration);

        var app = builder.Build();

        app.UseExceptionHandler();
        app.UseStatusCodePages();

        app.UseHttpsRedirection();

        app.MapGet("/health", () => Results.Ok(new
        {
            Status = "Healthy",
            Service = "FinanceHub.TransactionAggregator.Api",
            Timestamp = DateTime.UtcNow,
            Version = "1.0.0-net10"
        })).WithName("GetHealth");

        app.MapTransactionEndpoints();

        app.Run();
    }
}
