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
}
