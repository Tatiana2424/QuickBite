namespace QuickBite.Payments.Application;

public sealed record PaymentDto(Guid Id, Guid OrderId, decimal Amount, string Status, string? FailureReason);

public interface IPaymentReadService
{
    Task<PaymentDto?> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken);
}
