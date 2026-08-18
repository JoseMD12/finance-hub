using System.Security.Claims;
using FinanceHub.ApiGateway.Clients;
using FinanceHub.Shared.Messaging.Constants;
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
            HttpContext httpContext,
            IPluggyIntegrationServiceClient pluggyClient,
            CancellationToken ct) =>
        {
            var pluggyToken = httpContext.Request.Headers[FinanceHubHeaderNames.PluggyAccessToken].ToString();
            if (string.IsNullOrWhiteSpace(pluggyToken))
            {
                return Results.Problem(
                    title: "Erro de Negócio",
                    detail: $"O token de acesso do Meu.Pluggy (pluggyAccessToken / {FinanceHubHeaderNames.PluggyAccessToken}) é obrigatório para realizar a sincronização.",
                    statusCode: 400,
                    extensions: new Dictionary<string, object?> { { "errorCode", "NULL_OR_EMPTY_PLUGGY_ACCESS_TOKEN" } }
                );
            }

            var resolvedUserId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                              ?? user.FindFirst("sub")?.Value
                              ?? userId;

            var summary = await pluggyClient.TriggerSyncAsync(resolvedUserId, pluggyToken, ct);
            return Results.Ok(summary);
        })
        .WithName("TriggerPluggySync")
        .WithSummary("Dispara a sincronização de todas as contas e transações da Pluggy via BFF Gateway.");

        return app;
    }
}
