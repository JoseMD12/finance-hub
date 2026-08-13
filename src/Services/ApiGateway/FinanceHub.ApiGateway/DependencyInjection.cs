using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;

using FinanceHub.ApiGateway.Clients;
using FinanceHub.ApiGateway.Middleware;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;

namespace FinanceHub.ApiGateway;

public static class DependencyInjection
{
    public static IServiceCollection AddApiGatewayServices(this IServiceCollection services, IConfiguration configuration)
    {
        // 1. Exception Handling RFC 7807
        services.AddExceptionHandler<GlobalExceptionHandler>();
        services.AddProblemDetails();

        // 2. JWT Authentication
        var secretKey = Environment.GetEnvironmentVariable(GatewayConstants.Auth.JwtSecretKeyEnvVar)
                     ?? configuration[GatewayConstants.Auth.JwtSecretKeyEnvVar]
                     ?? "FinanceHubSuperSecretDevKeyWithAtLeast32BytesLength!";

        var issuer = Environment.GetEnvironmentVariable(GatewayConstants.Auth.JwtIssuerEnvVar)
                  ?? configuration[GatewayConstants.Auth.JwtIssuerEnvVar]
                  ?? GatewayConstants.Auth.DefaultIssuer;

        var audience = Environment.GetEnvironmentVariable(GatewayConstants.Auth.JwtAudienceEnvVar)
                    ?? configuration[GatewayConstants.Auth.JwtAudienceEnvVar]
                    ?? GatewayConstants.Auth.DefaultAudience;

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
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
                ClockSkew = TimeSpan.Zero
            };
        });

        services.AddAuthorization(options =>
        {
            options.AddPolicy("ReadScope", policy => policy.RequireClaim("scope", GatewayConstants.Scopes.Read));
            options.AddPolicy("WriteScope", policy => policy.RequireClaim("scope", GatewayConstants.Scopes.Write));
            options.AddPolicy("AdminScope", policy => policy.RequireClaim("scope", GatewayConstants.Scopes.Admin));
        });

        // 3. Rate Limiting
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

        // 4. Typed HttpClients for Downstream Microservices
        var authConsentUrl = Environment.GetEnvironmentVariable(GatewayConstants.Downstream.AuthConsentBaseUrlEnvVar)
                          ?? configuration[GatewayConstants.Downstream.AuthConsentBaseUrlEnvVar]
                          ?? GatewayConstants.Downstream.DefaultAuthConsentUrl;

        var transactionAggregatorUrl = Environment.GetEnvironmentVariable(GatewayConstants.Downstream.TransactionAggregatorBaseUrlEnvVar)
                                    ?? configuration[GatewayConstants.Downstream.TransactionAggregatorBaseUrlEnvVar]
                                    ?? GatewayConstants.Downstream.DefaultTransactionAggregatorUrl;

        services.AddHttpClient<IAuthConsentServiceClient, AuthConsentServiceClient>(client =>
        {
            client.BaseAddress = new Uri(authConsentUrl);
            client.Timeout = TimeSpan.FromSeconds(GatewayConstants.Downstream.DefaultTimeoutSeconds);
        });

        services.AddHttpClient<ITransactionAggregatorServiceClient, TransactionAggregatorServiceClient>(client =>
        {
            client.BaseAddress = new Uri(transactionAggregatorUrl);
            client.Timeout = TimeSpan.FromSeconds(GatewayConstants.Downstream.DefaultTimeoutSeconds);
        });

        // 5. Health Checks
        services.AddHealthChecks();

        return services;
    }
}
