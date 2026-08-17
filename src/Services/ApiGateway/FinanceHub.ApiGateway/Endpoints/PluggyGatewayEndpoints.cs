using System.Security.Claims;
using FinanceHub.ApiGateway.Clients;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FinanceHub.ApiGateway.Endpoints;

public static class PluggyGatewayEndpoints
{
    public static IEndpointRouteBuilder MapPluggyGatewayEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/gateway/pluggy")
            .WithTags("PluggyGateway");

        group.MapPost("/sync", async (
            ClaimsPrincipal user,
            string? userId,
            IPluggyIntegrationServiceClient pluggyClient,
            CancellationToken ct) =>
        {
            var resolvedUserId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                              ?? user.FindFirst("sub")?.Value
                              ?? userId;

            var summary = await pluggyClient.TriggerSyncAsync(resolvedUserId, ct);
            return Results.Ok(summary);
        })
        .WithName("TriggerPluggySync")
        .WithSummary("Dispara a sincronização de todas as contas e transações da Pluggy via BFF Gateway.");

        return app;
    }
}
