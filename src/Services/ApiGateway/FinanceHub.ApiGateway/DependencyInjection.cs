using System;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Threading.RateLimiting;

using FinanceHub.ApiGateway.Clients;
using FinanceHub.ApiGateway.Exceptions;
using FinanceHub.ApiGateway.Middleware;
using FinanceHub.ApiGateway.Services;
using FinanceHub.Shared.Observability.Exceptions.Mapping;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;

namespace FinanceHub.ApiGateway;

public static class DependencyInjection
{
    public static IServiceCollection AddApiGatewayServices(this IServiceCollection services, IConfiguration configuration)
    {
        // 1. Exception Handling RFC 7807 with Strategy Mappers
        services.AddExceptionMappingServices();
        services.AddExceptionHandler<GlobalExceptionHandler>();
        services.AddProblemDetails();

        // 2. RSA Asymmetric Key Registration (FAPI Profile)
        var rsaKey = CreateRsaSecurityKey(configuration);
        services.AddSingleton(rsaKey);
        services.AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();

        var issuer = Environment.GetEnvironmentVariable(GatewayConstants.Auth.JwtIssuerEnvVar)
                  ?? configuration[GatewayConstants.Auth.JwtIssuerEnvVar]
                  ?? throw new GatewayConfigurationException(GatewayConstants.Auth.JwtIssuerEnvVar);

        var audience = Environment.GetEnvironmentVariable(GatewayConstants.Auth.JwtAudienceEnvVar)
                    ?? configuration[GatewayConstants.Auth.JwtAudienceEnvVar]
                    ?? throw new GatewayConfigurationException(GatewayConstants.Auth.JwtAudienceEnvVar);

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.RequireHttpsMetadata = false;
            options.SaveToken = true;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = issuer,
                ValidAudience = audience,
                IssuerSigningKey = rsaKey,
                ClockSkew = TimeSpan.Zero
            };
        });

        services.AddAuthorization(options =>
        {
            options.AddPolicy("ReadScope", policy => policy.RequireClaim("scope", GatewayConstants.Scopes.Read));
            options.AddPolicy("WriteScope", policy => policy.RequireClaim("scope", GatewayConstants.Scopes.Write));
            options.AddPolicy("AdminScope", policy => policy.RequireClaim("scope", GatewayConstants.Scopes.Admin));
        });

        // 3. Rate Limiting Policy
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.AddFixedWindowLimiter(GatewayConstants.RateLimiting.AnonymousPolicy, opt =>
            {
                opt.PermitLimit = GatewayConstants.RateLimiting.AnonymousPermitLimit;
                opt.Window = TimeSpan.FromMinutes(1);
                opt.QueueLimit = 0;
            });

            options.AddPolicy(GatewayConstants.RateLimiting.AuthenticatedPolicy, httpContext =>
            {
                var userId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                          ?? httpContext.User.FindFirst("sub")?.Value
                          ?? httpContext.Connection.RemoteIpAddress?.ToString()
                          ?? "anonymous";

                return RateLimitPartition.GetSlidingWindowLimiter(userId, _ => new SlidingWindowRateLimiterOptions
                {
                    PermitLimit = GatewayConstants.RateLimiting.AuthenticatedPermitLimit,
                    Window = TimeSpan.FromMinutes(1),
                    SegmentsPerWindow = 4,
                    QueueLimit = 0
                });
            });
        });

        // 4. Downstream HttpClients
        var authConsentUrl = Environment.GetEnvironmentVariable(GatewayConstants.Downstream.AuthConsentBaseUrlEnvVar)
                          ?? configuration[GatewayConstants.Downstream.AuthConsentBaseUrlEnvVar]
                          ?? throw new GatewayConfigurationException(GatewayConstants.Downstream.AuthConsentBaseUrlEnvVar);

        var transactionAggregatorUrl = Environment.GetEnvironmentVariable(GatewayConstants.Downstream.TransactionAggregatorBaseUrlEnvVar)
                                    ?? configuration[GatewayConstants.Downstream.TransactionAggregatorBaseUrlEnvVar]
                                    ?? throw new GatewayConfigurationException(GatewayConstants.Downstream.TransactionAggregatorBaseUrlEnvVar);

        services.AddHttpClient<IAuthConsentServiceClient, AuthConsentServiceClient>(client =>
        {
            client.BaseAddress = new Uri(authConsentUrl);
            client.Timeout = TimeSpan.FromSeconds(GatewayConstants.Downstream.DefaultTimeoutSeconds);
        })
        .AddStandardResilienceHandler();

        services.AddHttpClient<ITransactionAggregatorServiceClient, TransactionAggregatorServiceClient>(client =>
        {
            client.BaseAddress = new Uri(transactionAggregatorUrl);
            client.Timeout = TimeSpan.FromSeconds(GatewayConstants.Downstream.DefaultTimeoutSeconds);
        })
        .AddStandardResilienceHandler();

        var pluggyIntegrationUrl = Environment.GetEnvironmentVariable(GatewayConstants.Downstream.PluggyIntegrationBaseUrlEnvVar)
                                ?? configuration[GatewayConstants.Downstream.PluggyIntegrationBaseUrlEnvVar]
                                ?? throw new GatewayConfigurationException(GatewayConstants.Downstream.PluggyIntegrationBaseUrlEnvVar);

        services.AddHttpClient<IPluggyIntegrationServiceClient, PluggyIntegrationServiceClient>(client =>
        {
            client.BaseAddress = new Uri(pluggyIntegrationUrl);
            client.Timeout = TimeSpan.FromSeconds(30);
        })
        .AddStandardResilienceHandler();

        // 5. Health Checks
        services.AddHealthChecks();

        return services;
    }

    private static RsaSecurityKey CreateRsaSecurityKey(IConfiguration configuration)
    {
        var pem = Environment.GetEnvironmentVariable("RSA_PRIVATE_KEY_PEM")
               ?? configuration["RSA_PRIVATE_KEY_PEM"];

        var rsa = RSA.Create();
        if (!string.IsNullOrWhiteSpace(pem))
        {
            rsa.ImportFromPem(pem);
        }
        else
        {
            rsa.KeySize = 2048;
        }

        return new RsaSecurityKey(rsa);
    }
}
