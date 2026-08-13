using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace FinanceHub.ApiGateway.Services;

public class JwtTokenGenerator : IJwtTokenGenerator
{
    private readonly SymmetricSecurityKey _securityKey;
    private readonly string _issuer;
    private readonly string _audience;

    public JwtTokenGenerator(IConfiguration configuration)
    {
        var rawKey = Environment.GetEnvironmentVariable(GatewayConstants.Auth.JwtSecretKeyEnvVar)
                  ?? configuration[GatewayConstants.Auth.JwtSecretKeyEnvVar];

        if (string.IsNullOrWhiteSpace(rawKey))
        {
            throw new InvalidOperationException($"A variável de ambiente '{GatewayConstants.Auth.JwtSecretKeyEnvVar}' é obrigatória.");
        }

        _issuer = Environment.GetEnvironmentVariable(GatewayConstants.Auth.JwtIssuerEnvVar)
               ?? configuration[GatewayConstants.Auth.JwtIssuerEnvVar]
               ?? throw new InvalidOperationException($"A variável de ambiente '{GatewayConstants.Auth.JwtIssuerEnvVar}' é obrigatória.");

        _audience = Environment.GetEnvironmentVariable(GatewayConstants.Auth.JwtAudienceEnvVar)
                 ?? configuration[GatewayConstants.Auth.JwtAudienceEnvVar]
                 ?? throw new InvalidOperationException($"A variável de ambiente '{GatewayConstants.Auth.JwtAudienceEnvVar}' é obrigatória.");

        _securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(rawKey));
    }

    public string GenerateDevToken(string userId)
    {
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, userId),
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim("scope", $"{GatewayConstants.Scopes.Read} {GatewayConstants.Scopes.Write}")
        };

        var signingCredentials = new SigningCredentials(_securityKey, SecurityAlgorithms.HmacSha256);

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
