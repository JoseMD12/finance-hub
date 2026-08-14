using System.Security.Claims;

using FinanceHub.ApiGateway.Clients;
using FinanceHub.ApiGateway.DTOs;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FinanceHub.ApiGateway.Endpoints;

public static class TransactionGatewayEndpoints
{
    public static IEndpointRouteBuilder MapTransactionGatewayEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/gateway/transactions")
            .WithTags("Gateway Transactions")
            .RequireAuthorization();

        group.MapGet("/", async (
            ClaimsPrincipal user,
            int? page,
            int? pageSize,
            ITransactionAggregatorServiceClient transactionClient,
            CancellationToken ct) =>
        {
            var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                      ?? user.FindFirst("sub")?.Value;

            if (string.IsNullOrWhiteSpace(userId))
            {
                return Results.Unauthorized();
            }

            var transactions = await transactionClient.GetTransactionsAsync(userId, page ?? 1, pageSize ?? 20, ct);
            return Results.Ok(transactions);
        })
        .WithName("GetGatewayTransactions")
        .Produces<IEnumerable<GatewayTransactionDto>>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized);

        group.MapPatch("/{id:guid}/category", async (
            Guid id,
            CategorizeRequest request,
            ClaimsPrincipal user,
            ITransactionAggregatorServiceClient transactionClient,
            CancellationToken ct) =>
        {
            var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                      ?? user.FindFirst("sub")?.Value;

            if (string.IsNullOrWhiteSpace(userId))
            {
                return Results.Unauthorized();
            }

            await transactionClient.CategorizeTransactionAsync(id, userId, request.CategoryId, request.CreateCustomRule, ct);
            return Results.NoContent();
        })
        .WithName("CategorizeGatewayTransaction")
        .Produces(StatusCodes.Status204NoContent)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status404NotFound);

        return endpoints;
    }

    public record CategorizeRequest(Guid CategoryId, bool CreateCustomRule);
}
