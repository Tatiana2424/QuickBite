using QuickBite.Identity.Domain;

namespace QuickBite.Identity.Tests;

public sealed class RefreshTokenTests
{
    [Fact]
    public void New_refresh_token_is_active_until_expired_or_revoked()
    {
        var token = new RefreshToken(Guid.NewGuid(), "HASH", DateTimeOffset.UtcNow.AddDays(1));

        Assert.True(token.IsActive);
        Assert.False(token.IsExpired);
        Assert.False(token.IsRevoked);
    }

    [Fact]
    public void Revoke_marks_refresh_token_inactive_and_tracks_replacement()
    {
        var token = new RefreshToken(Guid.NewGuid(), "OLD_HASH", DateTimeOffset.UtcNow.AddDays(1));

        token.Revoke("NEW_HASH");

        Assert.False(token.IsActive);
        Assert.True(token.IsRevoked);
        Assert.Equal("NEW_HASH", token.ReplacedByTokenHash);
    }

    [Fact]
    public void Expired_refresh_token_is_not_active()
    {
        var token = new RefreshToken(Guid.NewGuid(), "HASH", DateTimeOffset.UtcNow.AddMinutes(-1));

        Assert.True(token.IsExpired);
        Assert.False(token.IsActive);
    }
}
