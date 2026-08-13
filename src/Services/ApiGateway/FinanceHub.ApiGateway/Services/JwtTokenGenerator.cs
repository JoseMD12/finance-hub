using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace FinanceHub.ApiGateway.Services;

public class JwtTokenGenerator : IJwtTokenGenerator
{
    private readonly RsaSecurityKey _signingKey;
    private readonly string _issuer;
    private readonly string _audience;

    public JwtTokenGenerator(RsaSecurityKey signingKey, IConfiguration configuration)
    {
        _signingKey = signingKey ?? throw new ArgumentNullException(nameof(signingKey));

        _issuer = Environment.GetEnvironmentVariable(GatewayConstants.Auth.JwtIssuerEnvVar)
               ?? configuration[GatewayConstants.Auth.JwtIssuerEnvVar]
               ?? "https://financehub.local";

        _audience = Environment.GetEnvironmentVariable(GatewayConstants.Auth.JwtAudienceEnvVar)
                 ?? configuration[GatewayConstants.Auth.JwtAudienceEnvVar]
                 ?? "financehub-gateway";
    }

    public string GenerateDevToken(string userId)
    {
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, userId),
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim("scope", $"{GatewayConstants.Scopes.Read} {GatewayConstants.Scopes.Write}")
        };

        var signingCredentials = new SigningCredentials(_signingKey, SecurityAlgorithms.RsaSha256);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddHours(24),
            Issuer = _issuer,
            Audience = _audience,
            SigningCredentials = signingCredentials
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var securityToken = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(securityToken);
    }
}
