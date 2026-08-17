using DotNetEnv;

using FinanceHub.AuthConsent.Api.Endpoints;
using FinanceHub.AuthConsent.Infrastructure;
using FinanceHub.AuthConsent.Infrastructure.Persistence;
using FinanceHub.Shared.Observability;

namespace FinanceHub.AuthConsent.Api;

public class Program
{
    protected Program() { }

    public static async Task Main(string[] args)
    {
        Env.TraversePath().Load();

        var builder = WebApplication.CreateBuilder(args);

        builder.Host.UseFinanceHubSerilog();
        builder.Services.AddFinanceHubObservability(builder.Configuration, "FinanceHub.AuthConsent.Api");

        builder.Services.AddAuthConsentInfrastructure(builder.Configuration);
        builder.Services.AddAuthConsentApi(builder.Configuration);

        var app = builder.Build();

        using (var scope = app.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AuthConsentDbContext>();
            dbContext.Database.EnsureCreated();
        }

        app.UseExceptionHandler();
        app.UseHttpsRedirection();

        app.MapGet("/health", () => Results.Ok(new
        {
            Status = "Healthy",
            Service = "FinanceHub.AuthConsent.Api",
            Timestamp = DateTime.UtcNow,
            Version = "1.0.0-net10"
        })).WithName("GetHealth");

        app.MapConsentEndpoints();

        await app.RunAsync();
    }
}
