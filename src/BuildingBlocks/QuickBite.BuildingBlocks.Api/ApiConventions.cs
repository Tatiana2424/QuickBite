using System.Diagnostics;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace QuickBite.BuildingBlocks.Api;

public static class ApiConventions
{
    public const string CorrelationIdHeaderName = "X-Correlation-Id";
    public const string ApiVersionHeaderName = "X-QuickBite-Api-Version";
    public const string CurrentApiVersion = "1";

    public static IMvcBuilder AddQuickBiteApiDefaults(this IServiceCollection services)
    {
        services.Configure<ApiBehaviorOptions>(options =>
        {
            options.InvalidModelStateResponseFactory = context =>
                new ObjectResult(ApiProblemDetailsFactory.CreateValidationProblem(context.HttpContext, context.ModelState))
                {
                    StatusCode = StatusCodes.Status400BadRequest
                };
        });

        return services.AddControllers();
    }

    public static IApplicationBuilder UseQuickBiteExceptionHandling(this IApplicationBuilder app, IHostEnvironment environment)
    {
        app.UseExceptionHandler(errorApp =>
        {
            errorApp.Run(async context =>
            {
                var exceptionFeature = context.Features.Get<IExceptionHandlerFeature>();
                var logger = context.RequestServices
                    .GetRequiredService<ILoggerFactory>()
                    .CreateLogger("QuickBite.Api.ExceptionHandling");

                logger.LogError(exceptionFeature?.Error, "Unhandled API exception.");

                var problem = ApiProblemDetailsFactory.CreateProblem(
                    context,
                    StatusCodes.Status500InternalServerError,
                    "An unexpected error occurred.",
                    environment.IsDevelopment() ? exceptionFeature?.Error.Message : null);

                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                context.Response.ContentType = "application/problem+json";
                await context.Response.WriteAsJsonAsync(problem);
            });
        });

        return app;
    }

    public static IApplicationBuilder UseQuickBiteApiVersionHeader(this IApplicationBuilder app)
    {
        return app.Use(async (context, next) =>
        {
            context.Response.Headers[ApiVersionHeaderName] = CurrentApiVersion;
            await next();
        });
    }
}

public static class ApiProblemDetailsFactory
{
    public static ProblemDetails CreateProblem(
        HttpContext httpContext,
        int statusCode,
        string title,
        string? detail = null,
        string? type = null)
    {
        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Type = type ?? $"https://httpstatuses.com/{statusCode}",
            Instance = httpContext.Request.Path
        };

        AddCommonExtensions(problem, httpContext);
        return problem;
    }

    public static ValidationProblemDetails CreateValidationProblem(HttpContext httpContext, ModelStateDictionary modelState)
    {
        var problem = new ValidationProblemDetails(modelState)
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Request validation failed.",
            Type = "https://httpstatuses.com/400",
            Instance = httpContext.Request.Path
        };

        AddCommonExtensions(problem, httpContext);
        return problem;
    }

    public static ValidationProblemDetails CreateValidationProblem(
        HttpContext httpContext,
        IDictionary<string, string[]> errors)
    {
        var problem = new ValidationProblemDetails(errors)
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Request validation failed.",
            Type = "https://httpstatuses.com/400",
            Instance = httpContext.Request.Path
        };

        AddCommonExtensions(problem, httpContext);
        return problem;
    }

    private static void AddCommonExtensions(ProblemDetails problem, HttpContext httpContext)
    {
        problem.Extensions["traceId"] = Activity.Current?.Id ?? httpContext.TraceIdentifier;
        problem.Extensions["correlationId"] = GetCorrelationId(httpContext);
        problem.Extensions["apiVersion"] = ApiConventions.CurrentApiVersion;
        problem.Extensions["path"] = httpContext.Request.Path.Value ?? "/";
    }

    private static string GetCorrelationId(HttpContext httpContext)
    {
        return httpContext.Request.Headers[ApiConventions.CorrelationIdHeaderName].FirstOrDefault()
            ?? httpContext.Response.Headers[ApiConventions.CorrelationIdHeaderName].FirstOrDefault()
            ?? httpContext.TraceIdentifier;
    }
}
