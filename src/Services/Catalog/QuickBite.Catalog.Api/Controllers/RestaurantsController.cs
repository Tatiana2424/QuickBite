using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuickBite.BuildingBlocks.Api;
using QuickBite.Catalog.Application;

namespace QuickBite.Catalog.Api.Controllers;

[ApiController]
[Route("api/restaurants")]
public sealed class RestaurantsController(ICatalogService catalogService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<RestaurantSummaryDto>>> GetRestaurants(CancellationToken cancellationToken)
        => Ok(await catalogService.GetRestaurantsAsync(cancellationToken));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<RestaurantDetailsDto>> GetRestaurant(Guid id, CancellationToken cancellationToken)
    {
        var restaurant = await catalogService.GetRestaurantAsync(id, cancellationToken);
        return restaurant is null ? this.NotFoundProblem($"Restaurant '{id}' was not found.") : Ok(restaurant);
    }

    [HttpGet("{id:guid}/menu")]
    public async Task<ActionResult<IReadOnlyCollection<MenuItemDto>>> GetMenu(Guid id, CancellationToken cancellationToken)
        => Ok(await catalogService.GetMenuAsync(id, cancellationToken));

    [Authorize(Roles = "RestaurantAdmin,PlatformAdmin")]
    [HttpPost]
    public async Task<ActionResult<RestaurantDetailsDto>> CreateRestaurant(
        RestaurantMutationRequest request,
        CancellationToken cancellationToken)
    {
        var validation = ValidateRestaurant(request);
        if (validation.Count > 0)
        {
            return this.ValidationProblem(validation);
        }

        var restaurant = await catalogService.CreateRestaurantAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetRestaurant), new { id = restaurant.Id }, restaurant);
    }

    [Authorize(Roles = "RestaurantAdmin,PlatformAdmin")]
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<RestaurantDetailsDto>> UpdateRestaurant(
        Guid id,
        RestaurantMutationRequest request,
        CancellationToken cancellationToken)
    {
        var validation = ValidateRestaurant(request);
        if (validation.Count > 0)
        {
            return this.ValidationProblem(validation);
        }

        var restaurant = await catalogService.UpdateRestaurantAsync(id, request, cancellationToken);
        return restaurant is null ? this.NotFoundProblem($"Restaurant '{id}' was not found.") : Ok(restaurant);
    }

    [Authorize(Roles = "RestaurantAdmin,PlatformAdmin")]
    [HttpPost("{id:guid}/menu")]
    public async Task<ActionResult<MenuItemDto>> CreateMenuItem(
        Guid id,
        MenuItemMutationRequest request,
        CancellationToken cancellationToken)
    {
        var validation = ValidateMenuItem(request);
        if (validation.Count > 0)
        {
            return this.ValidationProblem(validation);
        }

        var menuItem = await catalogService.CreateMenuItemAsync(id, request, cancellationToken);
        return menuItem is null ? this.NotFoundProblem($"Restaurant '{id}' was not found.") : Ok(menuItem);
    }

    [Authorize(Roles = "RestaurantAdmin,PlatformAdmin")]
    [HttpPut("{id:guid}/menu/{menuItemId:guid}")]
    public async Task<ActionResult<MenuItemDto>> UpdateMenuItem(
        Guid id,
        Guid menuItemId,
        MenuItemMutationRequest request,
        CancellationToken cancellationToken)
    {
        var validation = ValidateMenuItem(request);
        if (validation.Count > 0)
        {
            return this.ValidationProblem(validation);
        }

        var menuItem = await catalogService.UpdateMenuItemAsync(id, menuItemId, request, cancellationToken);
        return menuItem is null ? this.NotFoundProblem($"Menu item '{menuItemId}' was not found.") : Ok(menuItem);
    }

    private static Dictionary<string, string[]> ValidateRestaurant(RestaurantMutationRequest request)
    {
        var errors = new Dictionary<string, string[]>();
        AddRequired(errors, nameof(request.Name), request.Name, 200);
        AddRequired(errors, nameof(request.Cuisine), request.Cuisine, 100);
        AddRequired(errors, nameof(request.Description), request.Description, 500);
        return errors;
    }

    private static Dictionary<string, string[]> ValidateMenuItem(MenuItemMutationRequest request)
    {
        var errors = new Dictionary<string, string[]>();
        AddRequired(errors, nameof(request.Name), request.Name, 200);
        AddRequired(errors, nameof(request.Description), request.Description, 500);
        if (request.Price <= 0)
        {
            errors[nameof(request.Price)] = ["Price must be greater than zero."];
        }

        return errors;
    }

    private static void AddRequired(Dictionary<string, string[]> errors, string field, string value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            errors[field] = [$"{field} is required."];
        }
        else if (value.Length > maxLength)
        {
            errors[field] = [$"{field} must be {maxLength} characters or fewer."];
        }
    }
}
