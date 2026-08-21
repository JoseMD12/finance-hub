using System.Security.Claims;
using FinanceHub.ApiGateway.Clients;
using FinanceHub.Shared.Messaging.Constants;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FinanceHub.ApiGateway.Endpoints;

public static class PluggyGatewayEndpoints
{
    private const string BusinessErrorTitle = "Erro de Negócio";
    private const string ErrorCodeKey = "errorCode";
    private const string NullOrEmptyPluggyAccessTokenErrorCode = "NULL_OR_EMPTY_PLUGGY_ACCESS_TOKEN";

    public static IEndpointRouteBuilder MapPluggyGatewayEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/gateway/pluggy")
            .WithTags("PluggyGateway");

        group.MapGet("/items", async (
            HttpContext httpContext,
            IPluggyIntegrationServiceClient pluggyClient,
            CancellationToken ct) =>
        {
            if (!TryGetPluggyToken(httpContext, "consultar as instituições", out var pluggyToken, out var errorResult))
            {
                return errorResult!;
            }

            var items = await pluggyClient.GetItemsAsync(pluggyToken, ct);
            return Results.Ok(items);
        })
        .WithName("GetPluggyGatewayItems")
        .RequireAuthorization("ReadScope")
        .WithSummary("Lista as instituições bancárias conectadas via Meu.Pluggy no BFF Gateway.");

        group.MapGet("/accounts", GetAccountsHandler)
            .WithName("GetPluggyGatewayAccounts")
            .AllowAnonymous()
            .WithSummary("Lista as contas bancárias conectadas via Meu.Pluggy no BFF Gateway.");

        app.MapGet("/api/v1/pluggy/accounts", GetAccountsHandler)
            .WithName("GetPluggyDirectAccounts")
            .AllowAnonymous()
            .WithSummary("Alias direto para consulta de contas conectadas da extensão via Meu.Pluggy.");

        group.MapPost("/items/{itemId}/sync", async (
            string itemId,
            ClaimsPrincipal user,
            HttpContext httpContext,
            IPluggyIntegrationServiceClient pluggyClient,
            CancellationToken ct) =>
        {
            if (!TryGetPluggyToken(httpContext, "ressincronizar a instituição", out var pluggyToken, out var errorResult))
            {
                return errorResult!;
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
            if (!TryGetPluggyToken(httpContext, "realizar a sincronização", out var pluggyToken, out var errorResult))
            {
                return errorResult!;
            }

            var resolvedUserId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                              ?? user.FindFirst("sub")?.Value
                              ?? userId;

            var job = await pluggyClient.TriggerSyncAsync(resolvedUserId, pluggyToken, ct);
            return job is not null
                ? Results.Accepted($"/api/v1/gateway/pluggy/sync/jobs/{job.JobId}", job)
                : Results.StatusCode(500);
        })
        .WithName("TriggerPluggySync")
        .RequireAuthorization("WriteScope")
        .WithSummary("Dispara a sincronização assíncrona (202 Accepted) de todas as contas e transações da Pluggy via BFF Gateway.");

        group.MapGet("/sync/jobs/{jobId:guid}", async (
            Guid jobId,
            IPluggyIntegrationServiceClient pluggyClient,
            CancellationToken ct) =>
        {
            var job = await pluggyClient.GetSyncJobStatusAsync(jobId, ct);
            return job is not null ? Results.Ok(job) : Results.NotFound();
        })
        .WithName("GetPluggyGatewaySyncJobStatus")
        .RequireAuthorization("ReadScope")
        .WithSummary("Consulta o status e o resultado de um job de sincronização assíncrono no BFF Gateway.");

        return app;
    }

    private static bool TryGetPluggyToken(HttpContext httpContext, string actionContext, out string token, out IResult? errorResult)
    {
        token = httpContext.Request.Headers[FinanceHubHeaderNames.PluggyAccessToken].ToString();
        if (string.IsNullOrWhiteSpace(token))
        {
            errorResult = Results.Problem(
                title: BusinessErrorTitle,
                detail: $"O token de acesso do Meu.Pluggy (pluggyAccessToken / {FinanceHubHeaderNames.PluggyAccessToken}) é obrigatório para {actionContext}.",
                statusCode: 400,
                extensions: new Dictionary<string, object?> { { ErrorCodeKey, NullOrEmptyPluggyAccessTokenErrorCode } }
            );
            return false;
        }

        errorResult = null;
        return true;
    }

    private static async Task<IResult> GetAccountsHandler(
        HttpContext httpContext,
        IPluggyIntegrationServiceClient pluggyClient,
        CancellationToken ct)
    {
        if (!TryGetPluggyToken(httpContext, "consultar as contas", out var pluggyToken, out var errorResult))
        {
            return errorResult!;
        }

        var accounts = await pluggyClient.GetAccountsAsync(pluggyToken, ct);
        return Results.Ok(accounts);
    }
}
