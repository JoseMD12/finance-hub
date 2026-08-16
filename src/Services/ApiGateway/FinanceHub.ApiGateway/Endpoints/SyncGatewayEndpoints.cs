using System.Security.Claims;
using FinanceHub.ApiGateway.Clients;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FinanceHub.ApiGateway.Endpoints;

public record GatewaySyncRequest(string ItemId, string? AccountId = null);
public record GatewayConnectTokenRequest(string? ItemId = null);

public static class SyncGatewayEndpoints
{
    public static IEndpointRouteBuilder MapSyncGatewayEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/gateway/mercadopago")
                             .WithTags("Gateway Mercado Pago Open Finance")
                             .RequireAuthorization();

        group.MapPost("/connect-token", async (
            GatewayConnectTokenRequest? request,
            ClaimsPrincipal user,
            IMercadoPagoServiceClient mercadoPagoClient,
            CancellationToken ct) =>
        {
            var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                      ?? user.FindFirst("sub")?.Value;

            if (string.IsNullOrWhiteSpace(userId))
            {
                return Results.Unauthorized();
            }

            var result = await mercadoPagoClient.CreateConnectTokenAsync(userId, request?.ItemId, ct);
            return Results.Ok(result);
        })
        .WithName("TriggerMercadoPagoConnectToken")
        .Produces<GatewayConnectTokenResultDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status502BadGateway);

        group.MapPost("/sync", async (
            GatewaySyncRequest request,
            ClaimsPrincipal user,
            IMercadoPagoServiceClient mercadoPagoClient,
            CancellationToken ct) =>
        {
            var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                      ?? user.FindFirst("sub")?.Value;

            if (string.IsNullOrWhiteSpace(userId))
            {
                return Results.Unauthorized();
            }

            if (string.IsNullOrWhiteSpace(request.ItemId))
            {
                return Results.BadRequest(new { error = "ItemId é obrigatório para sincronizar." });
            }

            var result = await mercadoPagoClient.TriggerSyncAsync(userId, request.ItemId, request.AccountId, ct);
            return Results.Accepted($"/api/v1/gateway/mercadopago/sync/{result.SyncId}", result);
        })
        .WithName("TriggerMercadoPagoSync")
        .Produces<GatewaySyncResultDto>(StatusCodes.Status202Accepted)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status502BadGateway);

        return endpoints;
    }
}
