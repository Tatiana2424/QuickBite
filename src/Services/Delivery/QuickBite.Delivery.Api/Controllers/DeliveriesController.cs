using Microsoft.AspNetCore.Mvc;
using QuickBite.BuildingBlocks.Api;
using QuickBite.Delivery.Application;

namespace QuickBite.Delivery.Api.Controllers;

[ApiController]
[Route("api/deliveries")]
public sealed class DeliveriesController(IDeliveryReadService deliveryReadService) : ControllerBase
{
    [HttpGet("{orderId:guid}")]
    public async Task<ActionResult<DeliveryDto>> GetByOrderId(Guid orderId, CancellationToken cancellationToken)
    {
        var delivery = await deliveryReadService.GetByOrderIdAsync(orderId, cancellationToken);
        return delivery is null ? this.NotFoundProblem($"Delivery for order '{orderId}' was not found.") : Ok(delivery);
    }
}
