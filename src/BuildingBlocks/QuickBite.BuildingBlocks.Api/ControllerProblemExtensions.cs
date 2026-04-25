using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace QuickBite.BuildingBlocks.Api;

public static class ControllerProblemExtensions
{
    public static ActionResult ValidationProblem(this ControllerBase controller, IDictionary<string, string[]> errors)
    {
        return new ObjectResult(ApiProblemDetailsFactory.CreateValidationProblem(
            controller.HttpContext,
            errors))
        {
            StatusCode = StatusCodes.Status400BadRequest
        };
    }

    public static ActionResult NotFoundProblem(this ControllerBase controller, string detail)
    {
        return new ObjectResult(ApiProblemDetailsFactory.CreateProblem(
            controller.HttpContext,
            StatusCodes.Status404NotFound,
            "Resource not found.",
            detail))
        {
            StatusCode = StatusCodes.Status404NotFound
        };
    }

    public static ActionResult ConflictProblem(this ControllerBase controller, string detail)
    {
        return new ObjectResult(ApiProblemDetailsFactory.CreateProblem(
            controller.HttpContext,
            StatusCodes.Status409Conflict,
            "Request conflicts with current state.",
            detail))
        {
            StatusCode = StatusCodes.Status409Conflict
        };
    }

    public static ActionResult UnauthorizedProblem(this ControllerBase controller, string detail)
    {
        return new ObjectResult(ApiProblemDetailsFactory.CreateProblem(
            controller.HttpContext,
            StatusCodes.Status401Unauthorized,
            "Authentication failed.",
            detail))
        {
            StatusCode = StatusCodes.Status401Unauthorized
        };
    }
}
