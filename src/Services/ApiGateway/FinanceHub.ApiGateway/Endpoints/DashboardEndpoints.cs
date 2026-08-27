using System.Security.Claims;

using FinanceHub.ApiGateway.Clients;
using FinanceHub.ApiGateway.DTOs;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FinanceHub.ApiGateway.Endpoints;

public static class DashboardEndpoints
{
    public static IEndpointRouteBuilder MapDashboardEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/gateway")
            .WithTags("Dashboard")
            .RequireAuthorization();

        group.MapGet("/dashboard", async (
            ClaimsPrincipal user,
            DateTime? startDate,
            DateTime? endDate,
            string? institutionId,
            bool? includeIgnoredInTotals,
            ITransactionAggregatorServiceClient transactionClient,
            CancellationToken ct) =>
        {
            var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                      ?? user.FindFirst("sub")?.Value;

            if (string.IsNullOrWhiteSpace(userId))
            {
                return Results.Unauthorized();
            }

            // userId vem da claim, jamais da query string: aceitar do cliente permitiria a um
            // usuário autenticado ler o Dashboard de outro.
            var filter = new GatewayDashboardFilterDto(
                UserId: userId,
                StartDate: startDate,
                EndDate: endDate,
                InstitutionId: institutionId,
                IncludeIgnoredInTotals: includeIgnoredInTotals ?? false);

            var summary = await transactionClient.GetDashboardSummaryAsync(filter, ct);

            return Results.Ok(summary);
        })
        .WithName("GetDashboard")
        .Produces<GatewayDashboardSummaryDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status502BadGateway);

        group.MapGet("/balances/consolidated", async (
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

            var balance = await transactionClient.GetConsolidatedBalanceAsync(userId, ct);
            return Results.Ok(balance);
        })
        .WithName("GetGatewayConsolidatedBalance")
        .Produces<GatewayConsolidatedBalanceDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized);

        return endpoints;
    }
}
