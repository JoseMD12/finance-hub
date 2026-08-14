using System.Security.Claims;

using FinanceHub.ApiGateway.Clients;
using FinanceHub.ApiGateway.DTOs;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FinanceHub.ApiGateway.Endpoints;

public static class ConsentGatewayEndpoints
{
    public static IEndpointRouteBuilder MapConsentGatewayEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/gateway/consents")
            .WithTags("Gateway Consents")
            .RequireAuthorization();

        group.MapGet("/", async (
            ClaimsPrincipal user,
            IAuthConsentServiceClient consentClient,
            CancellationToken ct) =>
        {
            var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                      ?? user.FindFirst("sub")?.Value;

            if (string.IsNullOrWhiteSpace(userId))
            {
                return Results.Unauthorized();
            }

            var consents = await consentClient.GetConsentsByUserIdAsync(userId, ct);
            return Results.Ok(consents);
        })
        .WithName("GetGatewayConsents")
        .Produces<IEnumerable<GatewayConsentDto>>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized);

        group.MapPost("/", async (
            GatewayCreateConsentRequest request,
            ClaimsPrincipal user,
            IAuthConsentServiceClient consentClient,
            CancellationToken ct) =>
        {
            var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                      ?? user.FindFirst("sub")?.Value;

            if (string.IsNullOrWhiteSpace(userId))
            {
                return Results.Unauthorized();
            }

            var consentId = await consentClient.CreateConsentAsync(userId, request.InstitutionId, request.ExternalConsentId, ct);
            return Results.Created($"/api/v1/gateway/consents/{consentId}", new { ConsentId = consentId });
        })
        .WithName("CreateGatewayConsent")
        .Produces(StatusCodes.Status201Created)
        .ProducesProblem(StatusCodes.Status401Unauthorized);

        group.MapPost("/{id:guid}/authorize", async (
            Guid id,
            GatewayAuthorizeConsentRequest request,
            IAuthConsentServiceClient consentClient,
            CancellationToken ct) =>
        {
            var consent = await consentClient.AuthorizeConsentAsync(id, request.AuthCode, request.RedirectUri, ct);
            return Results.Ok(consent);
        })
        .WithName("AuthorizeGatewayConsent")
        .Produces<GatewayConsentDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest);

        group.MapDelete("/{id:guid}", async (
            Guid id,
            IAuthConsentServiceClient consentClient,
            CancellationToken ct) =>
        {
            await consentClient.RevokeConsentAsync(id, ct);
            return Results.NoContent();
        })
        .WithName("RevokeGatewayConsent")
        .Produces(StatusCodes.Status204NoContent);

        return endpoints;
    }
}
