using Microsoft.AspNetCore.Http;
using QuickBite.BuildingBlocks.Api;

namespace QuickBite.BuildingBlocks.Tests;

public sealed class ApiProblemDetailsFactoryTests
{
    [Fact]
    public void CreateProblem_includes_correlation_id_and_api_version()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Path = "/api/orders/missing";
        httpContext.Request.Headers[ApiConventions.CorrelationIdHeaderName] = "test-correlation";

        var problem = ApiProblemDetailsFactory.CreateProblem(
            httpContext,
            StatusCodes.Status404NotFound,
            "Resource not found.",
            "Order was not found.");

        Assert.Equal(StatusCodes.Status404NotFound, problem.Status);
        Assert.Equal("/api/orders/missing", problem.Instance);
        Assert.Equal("test-correlation", problem.Extensions["correlationId"]);
        Assert.Equal(ApiConventions.CurrentApiVersion, problem.Extensions["apiVersion"]);
        Assert.Equal("/api/orders/missing", problem.Extensions["path"]);
    }

    [Fact]
    public void CreateValidationProblem_uses_standard_title_and_errors()
    {
        var httpContext = new DefaultHttpContext();
        var errors = new Dictionary<string, string[]>
        {
            ["email"] = ["Email is required."]
        };

        var problem = ApiProblemDetailsFactory.CreateValidationProblem(httpContext, errors);

        Assert.Equal(StatusCodes.Status400BadRequest, problem.Status);
        Assert.Equal("Request validation failed.", problem.Title);
        Assert.Equal("Email is required.", problem.Errors["email"].Single());
    }
}
