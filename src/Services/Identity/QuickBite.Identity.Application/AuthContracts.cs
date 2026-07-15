namespace QuickBite.Identity.Application;

public sealed record RegisterRequest(string Email, string FullName, string Password);
public sealed record LoginRequest(string Email, string Password);
public sealed record RefreshTokenRequest(string RefreshToken);
public sealed record RevokeRefreshTokenRequest(string RefreshToken);
public sealed record CustomerAddressDto(
    Guid Id,
    string Label,
    string Line1,
    string? Line2,
    string City,
    string State,
    string PostalCode,
    string Country,
    bool IsDefault);
public sealed record CustomerAddressRequest(
    string Label,
    string Line1,
    string? Line2,
    string City,
    string State,
    string PostalCode,
    string Country,
    bool IsDefault = false);
public sealed record AuthResponse(
    Guid UserId,
    string Email,
    string FullName,
    IReadOnlyCollection<string> Roles,
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAtUtc,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAtUtc);

public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken);
    Task<AuthResponse?> LoginAsync(LoginRequest request, CancellationToken cancellationToken);
    Task<AuthResponse?> RefreshAsync(RefreshTokenRequest request, CancellationToken cancellationToken);
    Task<bool> RevokeRefreshTokenAsync(RevokeRefreshTokenRequest request, CancellationToken cancellationToken);
}

public interface ICustomerAddressService
{
    Task<IReadOnlyCollection<CustomerAddressDto>> ListAsync(Guid userId, CancellationToken cancellationToken);
    Task<CustomerAddressDto> CreateAsync(Guid userId, CustomerAddressRequest request, CancellationToken cancellationToken);
    Task<CustomerAddressDto?> UpdateAsync(Guid userId, Guid addressId, CustomerAddressRequest request, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(Guid userId, Guid addressId, CancellationToken cancellationToken);
}
