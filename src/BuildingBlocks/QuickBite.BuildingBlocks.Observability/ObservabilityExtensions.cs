using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using QuickBite.BuildingBlocks.Common;
using Serilog;
using Serilog.AspNetCore;
using Serilog.Context;
using Serilog.Events;

namespace QuickBite.BuildingBlocks.Observability;

public static class ObservabilityExtensions
{
    public static void ConfigureQuickBiteObservability(this WebApplicationBuilder builder, string applicationName)
    {
        builder.Host.UseSerilog((context, _, loggerConfiguration) =>
            loggerConfiguration
                .ReadFrom.Configuration(context.Configuration)
                .Enrich.FromLogContext()
                .Enrich.WithProperty("Application", applicationName)
                .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
                .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database.Command", LogEventLevel.Warning)
                .MinimumLevel.Override("Yarp.ReverseProxy", LogEventLevel.Information)
                .WriteTo.Console());

        var resourceBuilder = ResourceBuilder.CreateDefault()
            .AddService(
                serviceName: applicationName,
                serviceVersion: typeof(ObservabilityExtensions).Assembly.GetName().Version?.ToString() ?? "0.0.0",
                serviceInstanceId: Environment.MachineName);

        builder.Services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(applicationName))
            .WithTracing(tracing =>
            {
                tracing
                    .SetResourceBuilder(resourceBuilder)
                    .AddSource(QuickBiteTelemetry.ActivitySourceName)
                    .AddAspNetCoreInstrumentation(options =>
                    {
                        options.RecordException = true;
                        options.Filter = context => !context.Request.Path.StartsWithSegments("/health")
                            && !context.Request.Path.StartsWithSegments("/metrics");
                    })
                    .AddHttpClientInstrumentation(options => options.RecordException = true);

                if (!string.IsNullOrWhiteSpace(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]))
                {
                    tracing.AddOtlpExporter();
                }
            })
            .WithMetrics(metrics =>
            {
                metrics
                    .SetResourceBuilder(resourceBuilder)
                    .AddMeter(QuickBiteTelemetry.MeterName)
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation()
                    .AddPrometheusExporter();

                if (!string.IsNullOrWhiteSpace(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]))
                {
                    metrics.AddOtlpExporter();
                }
            });
    }

    public static IApplicationBuilder UseQuickBiteObservability(this IApplicationBuilder app)
    {
        app.UseMiddleware<CorrelationIdMiddleware>();
        app.UseSerilogRequestLogging(ConfigureRequestLogging);

        if (app is WebApplication webApplication)
        {
            webApplication.MapPrometheusScrapingEndpoint("/metrics");
        }

        return app;
    }

    public static HealthCheckOptions QuickBiteHealthCheckOptions(
        Func<HealthCheckRegistration, bool>? predicate = null)
    {
        return new HealthCheckOptions
        {
            Predicate = predicate,
            ResponseWriter = WriteHealthResponseAsync
        };
    }

    public static IHealthChecksBuilder AddQuickBiteDbContextReadiness<TContext>(
        this IHealthChecksBuilder builder,
        string name)
        where TContext : DbContext
    {
        return builder.AddCheck<DbContextReadinessHealthCheck<TContext>>(
            name,
            failureStatus: HealthStatus.Unhealthy,
            tags: ["ready", "database"]);
    }

    private static void ConfigureRequestLogging(RequestLoggingOptions options)
    {
        options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
        options.GetLevel = (_, elapsed, exception) =>
            exception is not null ? LogEventLevel.Error :
            elapsed > 1_000 ? LogEventLevel.Warning :
            LogEventLevel.Information;
        options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
        {
            diagnosticContext.Set("TraceId", httpContext.TraceIdentifier);
            diagnosticContext.Set("CorrelationId", httpContext.Response.Headers["X-Correlation-Id"].FirstOrDefault() ?? httpContext.TraceIdentifier);
            diagnosticContext.Set("UserAgent", httpContext.Request.Headers.UserAgent.ToString());
            diagnosticContext.Set("ClientIp", httpContext.Connection.RemoteIpAddress?.ToString());
        };
    }

    private static async Task WriteHealthResponseAsync(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json";

        var response = new
        {
            status = report.Status.ToString(),
            totalDuration = report.TotalDuration.TotalMilliseconds,
            checks = report.Entries.Select(entry => new
            {
                name = entry.Key,
                status = entry.Value.Status.ToString(),
                duration = entry.Value.Duration.TotalMilliseconds,
                description = entry.Value.Description,
                error = entry.Value.Exception?.Message,
                tags = entry.Value.Tags
            })
        };

        await JsonSerializer.SerializeAsync(context.Response.Body, response, new JsonSerializerOptions(JsonSerializerDefaults.Web));
    }
}

internal sealed class DbContextReadinessHealthCheck<TContext>(IServiceProvider serviceProvider) : IHealthCheck
    where TContext : DbContext
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        await using var scope = serviceProvider.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TContext>();

        return await dbContext.Database.CanConnectAsync(cancellationToken)
            ? HealthCheckResult.Healthy("Database connection is available.")
            : HealthCheckResult.Unhealthy("Database connection is unavailable.");
    }
}

public sealed class CorrelationIdMiddleware
{
    private const string HeaderName = "X-Correlation-Id";
    private readonly RequestDelegate _next;

    public CorrelationIdMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers[HeaderName].FirstOrDefault() ?? Guid.NewGuid().ToString("N");
        context.Request.Headers[HeaderName] = correlationId;
        context.Response.Headers[HeaderName] = correlationId;
        context.TraceIdentifier = correlationId;

        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            await _next(context);
        }
    }
}
