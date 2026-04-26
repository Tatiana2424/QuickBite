using QuickBite.Payments.Domain;

namespace QuickBite.Payments.Tests;

public sealed class PaymentStatusHistoryTests
{
    [Fact]
    public void PaymentStatusHistory_records_payment_state_change()
    {
        var paymentId = Guid.NewGuid();

        var history = new PaymentStatusHistory(paymentId, PaymentStatus.Succeeded, "Approved");

        Assert.Equal(paymentId, history.PaymentId);
        Assert.Equal(PaymentStatus.Succeeded, history.Status);
        Assert.Equal("Approved", history.Reason);
    }
}
