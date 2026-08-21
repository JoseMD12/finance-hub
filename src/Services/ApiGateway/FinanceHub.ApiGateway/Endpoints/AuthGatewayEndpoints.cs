using FinanceHub.ApiGateway.DTOs;
using FinanceHub.ApiGateway.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Hosting;

namespace FinanceHub.ApiGateway.Endpoints;

public record DevTokenRequest(string UserId);

public record DevTokenResponse(string AccessToken, string TokenType, int ExpiresIn);

public static class AuthGatewayEndpoints
{
    public static IEndpointRouteBuilder MapAuthGatewayEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/gateway/auth")
            .WithTags("Gateway Auth");

        group.MapPost("/dev-token", (
            DevTokenRequest request,
            IJwtTokenGenerator tokenGenerator,
            IWebHostEnvironment env) =>
        {
            if (!env.IsDevelopment())
            {
                return Results.NotFound();
            }

            var userId = string.IsNullOrWhiteSpace(request.UserId) ? "usr_dev_001" : request.UserId;

            var tokenString = tokenGenerator.GenerateDevToken(userId);

            return Results.Ok(new DevTokenResponse(tokenString, "Bearer", 86400));
        })
        .WithName("GenerateDevToken")
        .AllowAnonymous();

        return endpoints;
    }
}
