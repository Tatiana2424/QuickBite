namespace QuickBite.Delivery.Application;

public sealed record DeliveryAddressDto(string Line1, string? Line2, string City, string State, string PostalCode, string Country);
public sealed record DeliveryDto(
    Guid Id,
    Guid OrderId,
    string Status,
    Guid CourierId,
    string CourierName,
    string CourierPhoneNumber,
    DeliveryAddressDto Address);

public interface IDeliveryReadService
{
    Task<DeliveryDto?> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken);
}
