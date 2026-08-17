using FinanceHub.PluggyIntegration.Application.Commands.SyncAllPluggyAccounts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FinanceHub.PluggyIntegration.Api.Endpoints;

public static class PluggyEndpoints
{
    public static IEndpointRouteBuilder MapPluggyEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/pluggy")
            .WithTags("PluggyIntegration");

        group.MapPost("/sync", async (
            string? userId,
            ISyncAllPluggyAccountsCommandHandler handler,
            CancellationToken cancellationToken) =>
        {
            var command = new SyncAllPluggyAccountsCommand(userId);
            var summary = await handler.HandleAsync(command, cancellationToken);
            return Results.Ok(summary);
        })
        .WithName("SyncPluggyAccounts")
        .WithSummary("Dispara a sincronização de todas as contas e transações via Meu.Pluggy.");

        return app;
    }
}
