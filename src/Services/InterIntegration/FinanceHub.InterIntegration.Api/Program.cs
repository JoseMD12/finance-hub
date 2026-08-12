var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

app.MapGet("/", () => "FinanceHub.InterIntegration API (.NET 10)");

app.Run();
