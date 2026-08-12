using FinanceHub.AuthConsent.Application.Commands.AuthorizeConsent;
using FinanceHub.AuthConsent.Application.DTOs;
using FinanceHub.AuthConsent.Application.Interfaces;

namespace FinanceHub.AuthConsent.Api.Endpoints;

public static class ConsentEndpoints
{
    public static void MapConsentEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/consents")
                       .WithTags("Consents");

        group.MapPost("/{id:guid}/authorize", async (
            Guid id,
            AuthorizeConsentRequest request,
            AuthorizeConsentCommandHandler handler,
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
    }
}

public record AuthorizeConsentRequest(string AuthCode, string RedirectUri);
