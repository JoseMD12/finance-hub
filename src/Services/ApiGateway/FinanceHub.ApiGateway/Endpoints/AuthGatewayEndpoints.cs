using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

using FinanceHub.ApiGateway.DTOs;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

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
            IConfiguration configuration) =>
        {
            var userId = string.IsNullOrWhiteSpace(request.UserId) ? "usr_dev_001" : request.UserId;

            var secretKey = configuration[GatewayConstants.Auth.JwtSecretKeyEnvVar]
                         ?? "FinanceHubSuperSecretDevKeyWithAtLeast32BytesLength!";

            var issuer = configuration[GatewayConstants.Auth.JwtIssuerEnvVar]
                      ?? GatewayConstants.Auth.DefaultIssuer;

            var audience = configuration[GatewayConstants.Auth.JwtAudienceEnvVar]
                        ?? GatewayConstants.Auth.DefaultAudience;

            var keyBytes = Encoding.UTF8.GetBytes(secretKey);
            var signingCredentials = new SigningCredentials(
                new SymmetricSecurityKey(keyBytes),
                SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, userId),
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim("scope", $"{GatewayConstants.Scopes.Read} {GatewayConstants.Scopes.Write}")
            };

            var expires = DateTime.UtcNow.AddHours(24);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = expires,
                Issuer = issuer,
                Audience = audience,
                SigningCredentials = signingCredentials
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var securityToken = tokenHandler.CreateToken(tokenDescriptor);
            var tokenString = tokenHandler.WriteToken(securityToken);

            return Results.Ok(new DevTokenResponse(tokenString, "Bearer", 86400));
        })
        .WithName("GenerateDevToken")
        .AllowAnonymous();

        return endpoints;
    }
}
