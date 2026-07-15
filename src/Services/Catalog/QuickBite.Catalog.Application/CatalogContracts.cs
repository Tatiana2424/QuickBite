namespace QuickBite.Catalog.Application;

public sealed record RestaurantSummaryDto(
    Guid Id,
    string Name,
    string Cuisine,
    string Description,
    string? ImageUrl,
    decimal Rating,
    int EstimatedDeliveryMinutes,
    decimal DeliveryFee,
    decimal MinimumOrder);
public sealed record MenuItemDto(Guid Id, Guid RestaurantId, string Name, string Description, decimal Price);
public sealed record RestaurantDetailsDto(
    Guid Id,
    string Name,
    string Cuisine,
    string Description,
    string? ImageUrl,
    decimal Rating,
    int EstimatedDeliveryMinutes,
    decimal DeliveryFee,
    decimal MinimumOrder,
    IReadOnlyCollection<MenuItemDto> MenuItems);
public sealed record RestaurantMutationRequest(string Name, string Cuisine, string Description);
public sealed record MenuItemMutationRequest(string Name, string Description, decimal Price);

public interface ICatalogService
{
    Task<IReadOnlyCollection<RestaurantSummaryDto>> GetRestaurantsAsync(CancellationToken cancellationToken);
    Task<RestaurantDetailsDto?> GetRestaurantAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<MenuItemDto>> GetMenuAsync(Guid restaurantId, CancellationToken cancellationToken);
    Task<RestaurantDetailsDto> CreateRestaurantAsync(RestaurantMutationRequest request, CancellationToken cancellationToken);
    Task<RestaurantDetailsDto?> UpdateRestaurantAsync(Guid id, RestaurantMutationRequest request, CancellationToken cancellationToken);
    Task<MenuItemDto?> CreateMenuItemAsync(Guid restaurantId, MenuItemMutationRequest request, CancellationToken cancellationToken);
    Task<MenuItemDto?> UpdateMenuItemAsync(Guid restaurantId, Guid menuItemId, MenuItemMutationRequest request, CancellationToken cancellationToken);
}
