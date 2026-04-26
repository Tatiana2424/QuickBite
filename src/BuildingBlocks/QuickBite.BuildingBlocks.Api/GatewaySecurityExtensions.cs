using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace QuickBite.BuildingBlocks.Api;

public static class GatewaySecurityExtensions
{
    private const string CorsPolicyName = "QuickBiteGatewayCors";

    public static IServiceCollection AddQuickBiteGatewaySecurity(this IServiceCollection services, IConfiguration configuration)
    {
        var allowedOrigins = configuration.GetSection("GatewaySecurity:AllowedOrigins").Get<string[]>()
            ?? ["http://localhost:3000"];

        services.AddCors(options =>
        {
            options.AddPolicy(CorsPolicyName, policy =>
            {
                policy.WithOrigins(allowedOrigins)
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
            });
        });

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
            {
                var partitionKey = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                return RateLimitPartition.GetFixedWindowLimiter(partitionKey, _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = configuration.GetValue("GatewaySecurity:RateLimiting:PermitLimit", 120),
                    Window = TimeSpan.FromMinutes(configuration.GetValue("GatewaySecurity:RateLimiting:WindowMinutes", 1)),
                    QueueLimit = 0,
                    AutoReplenishment = true
                });
            });
        });

        return services;
    }

    public static IApplicationBuilder UseQuickBiteGatewaySecurity(this IApplicationBuilder app)
    {
        app.UseSecurityHeaders();
        app.UseCors(CorsPolicyName);
        app.UseRateLimiter();
        return app;
    }

    private static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app)
    {
        return app.Use(async (context, next) =>
        {
            var headers = context.Response.Headers;
            headers["X-Content-Type-Options"] = "nosniff";
            headers["X-Frame-Options"] = "DENY";
            headers["Referrer-Policy"] = "no-referrer";
            headers["Content-Security-Policy"] = "default-src 'self'; frame-ancestors 'none'; object-src 'none'; base-uri 'self'";
            headers["Permissions-Policy"] = "geolocation=(), microphone=(), camera=()";

            await next();
        });
    }
}
