using DotNetEnv;

using FinanceHub.AuthConsent.Api.Endpoints;
using FinanceHub.AuthConsent.Infrastructure;
using FinanceHub.AuthConsent.Infrastructure.Persistence;
using FinanceHub.Shared.Messaging.Extensions;
using FinanceHub.Shared.Observability;

namespace FinanceHub.AuthConsent.Api;

public class Program
{
    public static void Main(string[] args)
    {
        Env.TraversePath().Load();

        var builder = WebApplication.CreateBuilder(args);

        builder.Host.UseFinanceHubSerilog();
        builder.Services.AddFinanceHubObservability(builder.Configuration, "FinanceHub.AuthConsent.Api");
        builder.Services.AddFinanceHubMessaging(builder.Configuration);

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

        app.Run();
    }
}
