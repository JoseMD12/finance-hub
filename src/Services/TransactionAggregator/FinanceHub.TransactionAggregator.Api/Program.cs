using System.Text.Json.Serialization;
using DotNetEnv;
using FinanceHub.Shared.Observability;
using FinanceHub.TransactionAggregator.Api;
using FinanceHub.TransactionAggregator.Api.Endpoints;
using FinanceHub.TransactionAggregator.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

Env.TraversePath().Load();

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseFinanceHubSerilog();
builder.Services.AddFinanceHubObservability(builder.Configuration, "FinanceHub.TransactionAggregator.Api");
builder.Services.AddTransactionAggregatorApiServices(builder.Configuration);

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<TransactionAggregatorDbContext>();
    dbContext.Database.Migrate();

    if (!dbContext.Categories.Any())
    {
        dbContext.Categories.AddRange(CategorySeedData.GetDefaultCategories());
        dbContext.SaveChanges();
    }
}

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
app.MapCategoryEndpoints();

await app.RunAsync();

namespace FinanceHub.TransactionAggregator.Api
{
    public partial class Program { }
}
