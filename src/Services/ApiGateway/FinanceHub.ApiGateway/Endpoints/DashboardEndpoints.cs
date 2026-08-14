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
            IAuthConsentServiceClient consentClient,
            ITransactionAggregatorServiceClient transactionClient,
            CancellationToken ct) =>
        {
            var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                      ?? user.FindFirst("sub")?.Value;

            if (string.IsNullOrWhiteSpace(userId))
            {
                return Results.Unauthorized();
            }

            var consentTask = consentClient.GetConsentsByUserIdAsync(userId, ct);
            var balanceTask = transactionClient.GetConsolidatedBalanceAsync(userId, ct);

            await Task.WhenAll(consentTask, balanceTask);

            var consents = await consentTask;
            var balance = await balanceTask;

            var response = new DashboardResponseDto(
                UserId: userId,
                TotalBalanceBrl: balance.TotalBalanceBrl,
                AccountBalances: balance.AccountBalances.Select(b => new AccountBalanceSummaryDto(
                    b.InstitutionId,
                    b.AccountNumber,
                    b.Amount,
                    b.Currency,
                    b.LastUpdatedAtUtc)),
                ActiveConsents: consents.Select(c => new ActiveConsentSummaryDto(
                    c.ConsentId,
                    c.InstitutionId,
                    c.Status,
                    c.ExpiresAtUtc)),
                GeneratedAtUtc: DateTime.UtcNow
            );

            return Results.Ok(response);
        })
        .WithName("GetDashboard")
        .Produces<DashboardResponseDto>(StatusCodes.Status200OK)
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
