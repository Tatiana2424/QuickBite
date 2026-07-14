using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
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

    [Authorize(Roles = "Courier,PlatformAdmin")]
    [HttpGet("courier/my")]
    public async Task<ActionResult<IReadOnlyCollection<DeliveryDto>>> ListMyDeliveries(CancellationToken cancellationToken)
    {
        if (!TryGetCourierName(out var courierName))
        {
            return this.UnauthorizedProblem("The current access token does not include a courier name.");
        }

        return Ok(await deliveryReadService.ListForCourierAsync(courierName, cancellationToken));
    }

    [Authorize(Roles = "Courier,PlatformAdmin")]
    [HttpGet("courier/my/{deliveryId:guid}")]
    public async Task<ActionResult<DeliveryDto>> GetMyDelivery(Guid deliveryId, CancellationToken cancellationToken)
    {
        if (!TryGetCourierName(out var courierName))
        {
            return this.UnauthorizedProblem("The current access token does not include a courier name.");
        }

        var delivery = await deliveryReadService.GetForCourierByIdAsync(courierName, deliveryId, cancellationToken);
        return delivery is null ? this.NotFoundProblem($"Delivery '{deliveryId}' was not found.") : Ok(delivery);
    }

    [Authorize(Roles = "Courier,PlatformAdmin")]
    [HttpPatch("courier/my/{deliveryId:guid}/status")]
    public async Task<ActionResult<DeliveryDto>> UpdateMyDeliveryStatus(
        Guid deliveryId,
        CourierDeliveryStatusRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Status))
        {
            return this.ValidationProblem(new Dictionary<string, string[]> { [nameof(request.Status)] = ["Status is required."] });
        }
        if (!IsSupportedCourierStatus(request.Status))
        {
            return this.ValidationProblem(new Dictionary<string, string[]> { [nameof(request.Status)] = ["Use Accepted, PickedUp, or Delivered."] });
        }

        if (!TryGetCourierName(out var courierName))
        {
            return this.UnauthorizedProblem("The current access token does not include a courier name.");
        }

        var delivery = await deliveryReadService.UpdateCourierDeliveryStatusAsync(courierName, deliveryId, request.Status, cancellationToken);
        return delivery is null ? this.NotFoundProblem($"Delivery '{deliveryId}' was not found.") : Ok(delivery);
    }

    private static bool IsSupportedCourierStatus(string status)
    {
        return status.Trim().ToLowerInvariant() is "accepted" or "pickedup" or "picked-up" or "picked up" or "delivered";
    }

    private bool TryGetCourierName(out string courierName)
    {
        courierName = User.FindFirstValue(JwtRegisteredClaimNames.Name)
            ?? User.FindFirstValue(ClaimTypes.Name)
            ?? string.Empty;

        return !string.IsNullOrWhiteSpace(courierName);
    }
}
