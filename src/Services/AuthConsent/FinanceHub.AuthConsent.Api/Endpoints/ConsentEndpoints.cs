using FinanceHub.AuthConsent.Application.Commands.AuthorizeConsent;
using FinanceHub.AuthConsent.Application.Commands.CreateConsent;
using FinanceHub.AuthConsent.Application.Commands.RenewToken;
using FinanceHub.AuthConsent.Application.Commands.RevokeConsent;
using FinanceHub.AuthConsent.Application.DTOs;
using FinanceHub.AuthConsent.Application.Queries.GetConsentByUserId;

namespace FinanceHub.AuthConsent.Api.Endpoints;

public static class ConsentEndpoints
{
    public static void MapConsentEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/consents")
                       .WithTags("Consents");

        group.MapPost("/", async (
            CreateConsentRequest request,
            ICreateConsentCommandHandler handler,
            CancellationToken ct) =>
        {
            var command = new CreateConsentCommand(request.UserId, request.InstitutionId, request.ExternalConsentId);
            var consentId = await handler.Handle(command, ct);
            return Results.Created($"/api/v1/consents/{consentId}", new { ConsentId = consentId });
        })
        .WithName("CreateConsent")
        .Produces<object>(StatusCodes.Status201Created)
        .ProducesProblem(StatusCodes.Status400BadRequest);

        group.MapPost("/{id:guid}/authorize", async (
            Guid id,
            AuthorizeConsentRequest request,
            IAuthorizeConsentCommandHandler handler,
            CancellationToken ct) =>
        {
            var command = new AuthorizeConsentCommand(id, request.AuthCode, request.RedirectUri);
            var result = await handler.Handle(command, ct);
            return Results.Ok(result);
        })
        .WithName("AuthorizeConsent")
        .Produces<ConsentResponseDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapGet("/user/{userId}", async (
            string userId,
            IGetConsentByUserIdQueryHandler handler,
            CancellationToken ct) =>
        {
            var query = new GetConsentByUserIdQuery(userId);
            var result = await handler.Handle(query, ct);
            return Results.Ok(result);
        })
        .WithName("GetConsentsByUserId")
        .Produces<IEnumerable<ConsentResponseDto>>(StatusCodes.Status200OK);

        group.MapPost("/{id:guid}/refresh", async (
            Guid id,
            IRenewTokenCommandHandler handler,
            CancellationToken ct) =>
        {
            var command = new RenewTokenCommand(id);
            var result = await handler.Handle(command, ct);
            return Results.Ok(result);
        })
        .WithName("RenewConsentToken")
        .Produces<OAuthTokenExchangeResult>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status401Unauthorized);

        group.MapDelete("/{id:guid}", async (
            Guid id,
            IRevokeConsentCommandHandler handler,
            CancellationToken ct) =>
        {
            var command = new RevokeConsentCommand(id);
            await handler.Handle(command, ct);
            return Results.NoContent();
        })
        .WithName("RevokeConsent")
        .Produces(StatusCodes.Status204NoContent)
        .ProducesProblem(StatusCodes.Status404NotFound);
    }
}

public record CreateConsentRequest(string UserId, string InstitutionId, string ExternalConsentId);
public record AuthorizeConsentRequest(string AuthCode, string RedirectUri);
