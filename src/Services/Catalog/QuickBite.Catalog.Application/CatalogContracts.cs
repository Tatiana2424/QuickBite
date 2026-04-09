namespace QuickBite.Catalog.Application;

public sealed record RestaurantSummaryDto(Guid Id, string Name, string Cuisine, string Description);
public sealed record MenuItemDto(Guid Id, Guid RestaurantId, string Name, string Description, decimal Price);
public sealed record RestaurantDetailsDto(Guid Id, string Name, string Cuisine, string Description, IReadOnlyCollection<MenuItemDto> MenuItems);

public interface ICatalogService
{
    Task<IReadOnlyCollection<RestaurantSummaryDto>> GetRestaurantsAsync(CancellationToken cancellationToken);
    Task<RestaurantDetailsDto?> GetRestaurantAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<MenuItemDto>> GetMenuAsync(Guid restaurantId, CancellationToken cancellationToken);
}
