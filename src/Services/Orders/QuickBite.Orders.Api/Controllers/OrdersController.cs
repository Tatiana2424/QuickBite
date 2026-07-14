using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuickBite.BuildingBlocks.Api;
using QuickBite.Orders.Application;

namespace QuickBite.Orders.Api.Controllers;

[ApiController]
[Route("api/orders")]
public sealed class OrdersController(IOrderService orderService) : ControllerBase
{
    [Authorize]
    [HttpPost]
    public async Task<ActionResult<OrderDto>> Create(
        [FromBody] CustomerCreateOrderRequest request,
        [FromServices] IValidator<CustomerCreateOrderRequest> validator,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return this.UnauthorizedProblem("The current access token does not include a user id.");
        }

        var validationResult = await validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return this.ValidationProblem(validationResult.ToDictionary());
        }

        var idempotencyKey = Request.Headers.TryGetValue("Idempotency-Key", out var values)
            ? values.ToString()
            : request.IdempotencyKey;

        var order = await orderService.CreateAsync(
            new CreateOrderRequest(userId, request.Items, request.DeliveryAddress, idempotencyKey),
            cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = order.Id }, order);
    }

    [Authorize]
    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<OrderDto>>> ListForCurrentUser(
        [FromQuery] int limit,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return this.UnauthorizedProblem("The current access token does not include a user id.");
        }

        var orders = await orderService.ListForUserAsync(userId, limit <= 0 ? 20 : limit, cancellationToken);
        return Ok(orders);
    }

    [Authorize]
    [HttpGet("my")]
    public async Task<ActionResult<OrderSummaryPageDto>> ListMyOrders(
        [FromQuery] int limit,
        [FromQuery] string? cursor,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return this.UnauthorizedProblem("The current access token does not include a user id.");
        }

        var orders = await orderService.ListSummariesForUserAsync(userId, limit <= 0 ? 20 : limit, cursor, cancellationToken);
        return Ok(orders);
    }

    [Authorize]
    [HttpGet("my/{id:guid}")]
    public async Task<ActionResult<OrderDetailsDto>> GetMyOrderById(Guid id, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return this.UnauthorizedProblem("The current access token does not include a user id.");
        }

        var order = await orderService.GetDetailsForUserByIdAsync(userId, id, cancellationToken);
        return order is null ? this.NotFoundProblem($"Order '{id}' was not found.") : Ok(order);
    }

    [Authorize]
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<OrderDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return this.UnauthorizedProblem("The current access token does not include a user id.");
        }

        var order = await orderService.GetForUserByIdAsync(userId, id, cancellationToken);
        return order is null ? this.NotFoundProblem($"Order '{id}' was not found.") : Ok(order);
    }

    private bool TryGetCurrentUserId(out Guid userId)
    {
        var rawUserId = User.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier);

        return Guid.TryParse(rawUserId, out userId);
    }
}

public sealed class CustomerCreateOrderRequestValidator : AbstractValidator<CustomerCreateOrderRequest>
{
    public CustomerCreateOrderRequestValidator()
    {
        RuleFor(x => x.Items).NotEmpty();
        RuleFor(x => x.DeliveryAddress).NotNull().SetValidator(new DeliveryAddressRequestValidator());
        RuleFor(x => x.IdempotencyKey).MaximumLength(120);
        RuleForEach(x => x.Items).SetValidator(new CreateOrderItemRequestValidator());
    }
}

public sealed class DeliveryAddressRequestValidator : AbstractValidator<DeliveryAddressRequest>
{
    public DeliveryAddressRequestValidator()
    {
        RuleFor(x => x.Line1).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Line2).MaximumLength(200);
        RuleFor(x => x.City).NotEmpty().MaximumLength(120);
        RuleFor(x => x.State).NotEmpty().MaximumLength(80);
        RuleFor(x => x.PostalCode).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Country).NotEmpty().MaximumLength(80);
    }
}

public sealed class CreateOrderItemRequestValidator : AbstractValidator<CreateOrderItemRequest>
{
    public CreateOrderItemRequestValidator()
    {
        RuleFor(x => x.MenuItemId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty();
        RuleFor(x => x.Quantity).GreaterThan(0);
        RuleFor(x => x.UnitPrice).GreaterThan(0);
    }
}
