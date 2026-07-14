namespace QuickBite.Orders.Application;

public sealed record CreateOrderItemRequest(Guid MenuItemId, string Name, int Quantity, decimal UnitPrice);
public sealed record CreateOrderRequest(Guid UserId, IReadOnlyCollection<CreateOrderItemRequest> Items, string? IdempotencyKey = null);
public sealed record OrderItemDto(Guid MenuItemId, string Name, int Quantity, decimal UnitPrice);
public sealed record OrderDto(
    Guid Id,
    Guid UserId,
    string Status,
    decimal TotalAmount,
    DateTimeOffset CreatedAtUtc,
    IReadOnlyCollection<OrderItemDto> Items);

public interface IOrderService
{
    Task<OrderDto> CreateAsync(CreateOrderRequest request, CancellationToken cancellationToken);
    Task<OrderDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<OrderDto?> GetForUserByIdAsync(Guid userId, Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<OrderDto>> ListForUserAsync(Guid userId, int limit, CancellationToken cancellationToken);
}
