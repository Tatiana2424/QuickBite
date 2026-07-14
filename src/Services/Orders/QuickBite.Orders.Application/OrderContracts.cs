namespace QuickBite.Orders.Application;

public sealed record CreateOrderItemRequest(Guid MenuItemId, string Name, int Quantity, decimal UnitPrice);
public sealed record DeliveryAddressRequest(string Line1, string? Line2, string City, string State, string PostalCode, string Country);
public sealed record DeliveryAddressDto(string Line1, string? Line2, string City, string State, string PostalCode, string Country);
public sealed record CustomerCreateOrderRequest(IReadOnlyCollection<CreateOrderItemRequest> Items, DeliveryAddressRequest DeliveryAddress, string? IdempotencyKey = null);
public sealed record CreateOrderRequest(Guid UserId, IReadOnlyCollection<CreateOrderItemRequest> Items, DeliveryAddressRequest DeliveryAddress, string? IdempotencyKey = null);
public sealed record OrderItemDto(Guid MenuItemId, string Name, int Quantity, decimal UnitPrice);
public sealed record OrderDto(
    Guid Id,
    Guid UserId,
    string Status,
    decimal TotalAmount,
    DateTimeOffset CreatedAtUtc,
    DeliveryAddressDto DeliveryAddress,
    IReadOnlyCollection<OrderItemDto> Items);
public sealed record OrderSummaryDto(
    Guid Id,
    string Status,
    decimal TotalAmount,
    DateTimeOffset CreatedAtUtc,
    int ItemCount,
    string ItemSummary);
public sealed record OrderDetailsDto(
    Guid Id,
    Guid UserId,
    string Status,
    decimal TotalAmount,
    DateTimeOffset CreatedAtUtc,
    DeliveryAddressDto DeliveryAddress,
    IReadOnlyCollection<OrderItemDto> Items);
public sealed record OrderSummaryPageDto(IReadOnlyCollection<OrderSummaryDto> Items, string? NextCursor);

public interface IOrderService
{
    Task<OrderDto> CreateAsync(CreateOrderRequest request, CancellationToken cancellationToken);
    Task<OrderDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<OrderDto?> GetForUserByIdAsync(Guid userId, Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<OrderDto>> ListForUserAsync(Guid userId, int limit, CancellationToken cancellationToken);
    Task<OrderDetailsDto?> GetDetailsForUserByIdAsync(Guid userId, Guid id, CancellationToken cancellationToken);
    Task<OrderSummaryPageDto> ListSummariesForUserAsync(Guid userId, int limit, string? cursor, CancellationToken cancellationToken);
}
