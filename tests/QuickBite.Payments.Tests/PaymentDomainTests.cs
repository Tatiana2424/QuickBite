using QuickBite.Payments.Domain;

namespace QuickBite.Payments.Tests;

public sealed class PaymentDomainTests
{
    [Fact]
    public void Payment_records_failed_provider_result()
    {
        var payment = new Payment(Guid.NewGuid(), 250m, PaymentStatus.Failed, "Rejected by simulated provider.");

        Assert.Equal(PaymentStatus.Failed, payment.Status);
        Assert.Equal(250m, payment.Amount);
        Assert.Equal("Rejected by simulated provider.", payment.FailureReason);
    }
}
