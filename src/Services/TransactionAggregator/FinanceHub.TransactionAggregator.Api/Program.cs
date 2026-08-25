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
    await dbContext.Database.MigrateAsync();

    if (!await dbContext.Categories.AnyAsync())
    {
        await dbContext.Categories.AddRangeAsync(CategorySeedData.GetDefaultCategories());
        await dbContext.SaveChangesAsync();
    }

    // Backfill de neutralidade e pareamento automático para transações existentes
    var transferCategoryId = Guid.Parse("11111111-1111-1111-1111-111111111002");
    var billPaymentCategoryId = Guid.Parse("11111111-1111-1111-1111-111111110801");
    var investmentsCategoryId = Guid.Parse("11111111-1111-1111-1111-111111110805");

    var candidateTransfers = await dbContext.Transactions
        .Where(t => (t.CategoryId == transferCategoryId || t.CategoryId == investmentsCategoryId) && !t.IsIgnoredInTotals)
        .ToListAsync();

    foreach (var tx in candidateTransfers)
    {
        tx.ToggleIgnoreInTotals(true);
    }

    var billPayments = await dbContext.Transactions
        .Where(t => t.CategoryId == billPaymentCategoryId && t.Description.CleanText.ToLower().Contains("fatura") && !t.IsIgnoredInTotals)
        .ToListAsync();

    foreach (var bp in billPayments)
    {
        bp.MarkAsBillPayment();
    }

    if (candidateTransfers.Count > 0 || billPayments.Count > 0)
    {
        await dbContext.SaveChangesAsync();
    }

    var matchingEngine = scope.ServiceProvider.GetRequiredService<FinanceHub.TransactionAggregator.Application.Interfaces.ITransferPairMatchingEngine>();
    var userIds = await dbContext.Transactions.Select(t => t.UserId).Distinct().ToListAsync();
    foreach (var uid in userIds)
    {
        await matchingEngine.MatchAndPairAsync(uid);
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
