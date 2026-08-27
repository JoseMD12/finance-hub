using System.Text.Json.Serialization;
using DotNetEnv;
using FinanceHub.Shared.Observability;
using FinanceHub.TransactionAggregator.Api;
using FinanceHub.TransactionAggregator.Api.Endpoints;
using FinanceHub.TransactionAggregator.Infrastructure.Persistence;
using FinanceHub.TransactionAggregator.Domain.Entities;
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

    // Backfill de neutralidade para transações já ingeridas antes de a natureza passar a ser
    // resolvida pela categoria. Usa exatamente a mesma regra do handler de ingestão — as
    // naturezas declaradas no catálogo — em vez de repetir identificadores de categoria soltos.
    var neutralCategoryIds = await dbContext.Categories
        .AsNoTracking()
        .Where(c => c.Nature != TransactionNature.Operating)
        .Select(c => c.Id)
        .ToListAsync();

    if (neutralCategoryIds.Count > 0)
    {
        // ExecuteUpdate roda no banco: não materializa as transações nem carrega o change tracker,
        // o que importa porque isto executa a cada boot sobre a tabela inteira.
        await dbContext.Transactions
            .Where(t => neutralCategoryIds.Contains(t.CategoryId) && !t.IsIgnoredInTotals)
            .ExecuteUpdateAsync(setters => setters.SetProperty(t => t.IsIgnoredInTotals, true));
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
app.MapDashboardEndpoints();

await app.RunAsync();

namespace FinanceHub.TransactionAggregator.Api
{
    public partial class Program { }
}
