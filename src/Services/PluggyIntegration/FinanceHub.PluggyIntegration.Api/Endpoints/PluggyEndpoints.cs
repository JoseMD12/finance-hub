using FinanceHub.PluggyIntegration.Application.Commands.SyncAllPluggyAccounts;
using FinanceHub.PluggyIntegration.Domain.Constants;
using FinanceHub.PluggyIntegration.Domain.Exceptions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FinanceHub.PluggyIntegration.Api.Endpoints;

public static class PluggyEndpoints
{
    public static IEndpointRouteBuilder MapPluggyEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/pluggy")
            .WithTags("PluggyIntegration");

        group.MapPost("/sync", async (
            string? userId,
            HttpContext httpContext,
            ISyncAllPluggyAccountsCommandHandler handler,
            CancellationToken cancellationToken) =>
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Results.BadRequest(new { error = "UserId é obrigatório para sincronização." });
            }

            var pluggyToken = httpContext.Request.Headers[PluggyConstants.HeaderNames.PluggyAccessToken].ToString();
            if (string.IsNullOrWhiteSpace(pluggyToken))
            {
                throw new NullOrEmptyPluggyAccessTokenDomainException();
            }

            var command = new SyncAllPluggyAccountsCommand(userId, pluggyToken);
            var summary = await handler.HandleAsync(command, cancellationToken);
            return Results.Ok(summary);
        })
        .WithName("SyncPluggyAccounts")
        .WithSummary("Dispara a sincronização de todas as contas e transações via Meu.Pluggy.");

        return app;
    }
}
