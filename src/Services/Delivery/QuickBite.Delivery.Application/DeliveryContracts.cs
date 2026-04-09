namespace QuickBite.Delivery.Application;

public sealed record DeliveryDto(Guid Id, Guid OrderId, string Status, Guid CourierId, string CourierName, string CourierPhoneNumber);

public interface IDeliveryReadService
{
    Task<DeliveryDto?> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken);
}
