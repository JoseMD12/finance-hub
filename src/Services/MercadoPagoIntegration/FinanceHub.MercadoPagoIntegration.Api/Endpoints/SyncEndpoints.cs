using FinanceHub.MercadoPagoIntegration.Application.Commands.CreateConnectToken;
using FinanceHub.MercadoPagoIntegration.Application.Commands.SyncTransactions;
using FinanceHub.MercadoPagoIntegration.Application.DTOs;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FinanceHub.MercadoPagoIntegration.Api.Endpoints;

public record CreateConnectTokenRequest(string UserId, string? ItemId = null);
public record SyncOpenFinanceRequest(string UserId, string ItemId, string? AccountId = null);

public static class SyncEndpoints
{
    public static IEndpointRouteBuilder MapSyncEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/mercadopago")
                             .WithTags("Mercado Pago Open Finance");

        group.MapPost("/connect-token", async (
            CreateConnectTokenRequest request,
            ICreateConnectTokenCommandHandler handler,
            CancellationToken ct) =>
        {
            var command = new CreateConnectTokenCommand(request.UserId, request.ItemId);
            var result = await handler.Handle(command, ct);
            return Results.Ok(result);
        })
        .WithName("CreateMercadoPagoConnectToken")
        .Produces<ConnectTokenDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status502BadGateway);

        group.MapPost("/sync", async (
            SyncOpenFinanceRequest request,
            ISyncMercadoPagoOpenFinanceCommandHandler handler,
            CancellationToken ct) =>
        {
            var command = new SyncMercadoPagoOpenFinanceCommand(request.UserId, request.ItemId, request.AccountId);
            var result = await handler.Handle(command, ct);
            return Results.Accepted($"/api/v1/mercadopago/sync/{result.SyncId}", result);
        })
        .WithName("SyncMercadoPagoOpenFinance")
        .Produces<OpenFinanceSyncResultDto>(StatusCodes.Status202Accepted)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .ProducesProblem(StatusCodes.Status502BadGateway);

        return endpoints;
    }
}
