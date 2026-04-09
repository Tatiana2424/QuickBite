using Microsoft.AspNetCore.Mvc;
using QuickBite.Payments.Application;

namespace QuickBite.Payments.Api.Controllers;

[ApiController]
[Route("api/payments")]
public sealed class PaymentsController(IPaymentReadService paymentReadService) : ControllerBase
{
    [HttpGet("{orderId:guid}")]
    public async Task<ActionResult<PaymentDto>> GetByOrderId(Guid orderId, CancellationToken cancellationToken)
    {
        var payment = await paymentReadService.GetByOrderIdAsync(orderId, cancellationToken);
        return payment is null ? NotFound() : Ok(payment);
    }
}
