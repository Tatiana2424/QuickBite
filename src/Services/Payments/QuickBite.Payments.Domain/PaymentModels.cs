using QuickBite.BuildingBlocks.Common;

namespace QuickBite.Payments.Domain;

public enum PaymentStatus
{
    Pending = 0,
    Succeeded = 1,
    Failed = 2
}

public sealed class Payment : Entity
{
    public Guid OrderId { get; private set; }
    public decimal Amount { get; private set; }
    public PaymentStatus Status { get; private set; }
    public string? FailureReason { get; private set; }

    private Payment()
    {
    }

    public Payment(Guid orderId, decimal amount, PaymentStatus status, string? failureReason = null)
    {
        OrderId = orderId;
        Amount = amount;
        Status = status;
        FailureReason = failureReason;
    }
}

public sealed class PaymentStatusHistory : Entity
{
    public Guid PaymentId { get; private set; }
    public PaymentStatus Status { get; private set; }
    public string Reason { get; private set; } = string.Empty;
    public DateTimeOffset ChangedAtUtc { get; private set; }
    public Payment? Payment { get; private set; }

    private PaymentStatusHistory()
    {
    }

    public PaymentStatusHistory(Guid paymentId, PaymentStatus status, string reason)
    {
        PaymentId = paymentId;
        Status = status;
        Reason = reason;
        ChangedAtUtc = DateTimeOffset.UtcNow;
    }
}
