using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuickBite.BuildingBlocks.Api;
using QuickBite.Identity.Application;

namespace QuickBite.Identity.Api.Controllers;

[ApiController]
[Authorize(Policy = "Customers")]
[Route("api/account")]
public sealed class AccountController(ICustomerAddressService addressService) : ControllerBase
{
    [HttpGet("addresses")]
    public async Task<ActionResult<IReadOnlyCollection<CustomerAddressDto>>> ListAddresses(CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return this.UnauthorizedProblem("Sign in again to manage saved addresses.");
        }

        return Ok(await addressService.ListAsync(userId, cancellationToken));
    }

    [HttpPost("addresses")]
    public async Task<ActionResult<CustomerAddressDto>> CreateAddress(
        [FromBody] CustomerAddressRequest request,
        [FromServices] IValidator<CustomerAddressRequest> validator,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return this.UnauthorizedProblem("Sign in again to save an address.");
        }

        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return this.ValidationProblem(validation.ToDictionary());
        }

        var address = await addressService.CreateAsync(userId, request, cancellationToken);
        return CreatedAtAction(nameof(ListAddresses), new { id = address.Id }, address);
    }

    [HttpPut("addresses/{addressId:guid}")]
    public async Task<ActionResult<CustomerAddressDto>> UpdateAddress(
        Guid addressId,
        [FromBody] CustomerAddressRequest request,
        [FromServices] IValidator<CustomerAddressRequest> validator,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return this.UnauthorizedProblem("Sign in again to update saved addresses.");
        }

        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return this.ValidationProblem(validation.ToDictionary());
        }

        var address = await addressService.UpdateAsync(userId, addressId, request, cancellationToken);
        return address is null ? this.NotFoundProblem($"Address '{addressId}' was not found.") : Ok(address);
    }

    [HttpDelete("addresses/{addressId:guid}")]
    public async Task<IActionResult> DeleteAddress(Guid addressId, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return this.UnauthorizedProblem("Sign in again to remove saved addresses.");
        }

        return await addressService.DeleteAsync(userId, addressId, cancellationToken)
            ? NoContent()
            : this.NotFoundProblem($"Address '{addressId}' was not found.");
    }

    private bool TryGetCurrentUserId(out Guid userId)
    {
        var rawUserId = User.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier);

        return Guid.TryParse(rawUserId, out userId);
    }
}

public sealed class CustomerAddressRequestValidator : AbstractValidator<CustomerAddressRequest>
{
    public CustomerAddressRequestValidator()
    {
        RuleFor(x => x.Label).NotEmpty().MaximumLength(80);
        RuleFor(x => x.Line1).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Line2).MaximumLength(200);
        RuleFor(x => x.City).NotEmpty().MaximumLength(120);
        RuleFor(x => x.State).NotEmpty().MaximumLength(80);
        RuleFor(x => x.PostalCode).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Country).NotEmpty().MaximumLength(80);
    }
}
