using System;
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
            [AsParameters] TransactionGatewayQueryParameters query,
            ITransactionAggregatorServiceClient transactionClient,
            CancellationToken ct) =>
        {
            var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                      ?? user.FindFirst("sub")?.Value;

            if (string.IsNullOrWhiteSpace(userId))
            {
                return Results.Unauthorized();
            }

            var filter = new GatewayTransactionFilterDto(
                userId,
                query.Page ?? 1,
                query.PageSize ?? 20,
                query.StartDate,
                query.EndDate,
                query.InstitutionId,
                query.CategoryId,
                query.Type,
                query.Search,
                query.IncludeIgnoredInTotals ?? false);

            var result = await transactionClient.GetTransactionsAsync(filter, ct);
            return Results.Ok(result);
        })
        .WithName("GetGatewayTransactions")
        .Produces<PagedGatewayTransactionsDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized);

        group.MapGet("/categories", async (
            ITransactionAggregatorServiceClient transactionClient,
            CancellationToken ct) =>
        {
            var categories = await transactionClient.GetCategoriesAsync(ct);
            return Results.Ok(categories);
        })
        .WithName("GetGatewayCategories")
        .AllowAnonymous()
        .Produces<IEnumerable<GatewayCategoryDto>>(StatusCodes.Status200OK);

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

            await transactionClient.CategorizeTransactionAsync(id, userId, request.CategoryId, request.CreateCustomRule, request.ApplyToPastTransactions, ct);
            return Results.NoContent();
        })
        .WithName("CategorizeGatewayTransaction")
        .Produces(StatusCodes.Status204NoContent)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPatch("/{id:guid}/neutrality", async (
            Guid id,
            ToggleNeutralityRequest request,
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

            await transactionClient.ToggleTransactionNeutralityAsync(id, userId, request.IsIgnoredInTotals, request.Reason, ct);
            return Results.NoContent();
        })
        .WithName("ToggleGatewayTransactionNeutrality")
        .Produces(StatusCodes.Status204NoContent)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPatch("/{id:guid}/bill-payment", async (
            Guid id,
            ToggleBillPaymentRequest request,
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

            await transactionClient.ToggleBillPaymentAsync(id, userId, request.IsBillPayment, ct);
            return Results.NoContent();
        })
        .WithName("ToggleGatewayTransactionBillPayment")
        .Produces(StatusCodes.Status204NoContent)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPatch("/{id:guid}/notes", async (
            Guid id,
            UpdateNotesRequest request,
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

            await transactionClient.UpdateTransactionNotesAsync(id, userId, request.Notes, ct);
            return Results.NoContent();
        })
        .WithName("UpdateGatewayTransactionNotes")
        .Produces(StatusCodes.Status204NoContent)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status404NotFound);

        return endpoints;
    }

    public record CategorizeRequest(Guid CategoryId, bool CreateCustomRule, bool ApplyToPastTransactions = false);
    public record ToggleNeutralityRequest(bool IsIgnoredInTotals, string? Reason = null);
    public record ToggleBillPaymentRequest(bool IsBillPayment);
    public record UpdateNotesRequest(string? Notes);
}
