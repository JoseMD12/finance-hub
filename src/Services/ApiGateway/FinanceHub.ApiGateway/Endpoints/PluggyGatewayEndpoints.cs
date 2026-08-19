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

        group.MapGet("/items", async (
            HttpContext httpContext,
            IPluggyIntegrationServiceClient pluggyClient,
            CancellationToken ct) =>
        {
            var pluggyToken = httpContext.Request.Headers[FinanceHubHeaderNames.PluggyAccessToken].ToString();
            if (string.IsNullOrWhiteSpace(pluggyToken))
            {
                return Results.Problem(
                    title: "Erro de Negócio",
                    detail: $"O token de acesso do Meu.Pluggy (pluggyAccessToken / {FinanceHubHeaderNames.PluggyAccessToken}) é obrigatório para consultar as instituições.",
                    statusCode: 400,
                    extensions: new Dictionary<string, object?> { { "errorCode", "NULL_OR_EMPTY_PLUGGY_ACCESS_TOKEN" } }
                );
            }

            var items = await pluggyClient.GetItemsAsync(pluggyToken, ct);
            return Results.Ok(items);
        })
        .WithName("GetPluggyGatewayItems")
        .RequireAuthorization("ReadScope")
        .WithSummary("Lista as instituições bancárias conectadas via Meu.Pluggy no BFF Gateway.");

        group.MapPost("/items/{itemId}/sync", async (
            string itemId,
            ClaimsPrincipal user,
            HttpContext httpContext,
            IPluggyIntegrationServiceClient pluggyClient,
            CancellationToken ct) =>
        {
            var pluggyToken = httpContext.Request.Headers[FinanceHub.Shared.Messaging.Constants.FinanceHubHeaderNames.PluggyAccessToken].ToString();
            if (string.IsNullOrWhiteSpace(pluggyToken))
            {
                return Results.Problem(
                    title: "Erro de Negócio",
                    detail: "O token de acesso do Meu.Pluggy é obrigatório para ressincronizar a instituição.",
                    statusCode: 400);
            }

            var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                       ?? user.FindFirst("sub")?.Value;
            var summary = await pluggyClient.ResyncItemAsync(itemId, userId, pluggyToken, ct);
            return Results.Ok(summary);
        })
        .WithName("ResyncPluggyGatewayItem")
        .RequireAuthorization("WriteScope")
        .WithSummary("Solicita uma nova sincronização para uma instituição específica.");

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
        .RequireAuthorization("WriteScope")
        .WithSummary("Dispara a sincronização de todas as contas e transações da Pluggy via BFF Gateway.");

        return app;
    }
}
