using Microsoft.Extensions.Options;
using QuickBite.Identity.Infrastructure;

namespace QuickBite.BuildingBlocks.Tests;

public sealed class JwtOptionsValidatorTests
{
    private readonly JwtOptionsValidator _validator = new();

    [Fact]
    public void Validate_returns_success_for_complete_options()
    {
        var result = _validator.Validate(Options.DefaultName, new JwtOptions
        {
            Issuer = "QuickBite",
            Audience = "QuickBite.Web",
            Key = "quickbite-super-secret-development-key-change-me"
        });

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void Validate_fails_when_key_is_too_short()
    {
        var result = _validator.Validate(Options.DefaultName, new JwtOptions
        {
            Issuer = "QuickBite",
            Audience = "QuickBite.Web",
            Key = "short-key"
        });

        Assert.False(result.Succeeded);
        Assert.Contains("at least 32 characters", result.FailureMessage);
    }

    [Fact]
    public void Validate_fails_when_required_fields_are_missing()
    {
        var result = _validator.Validate(Options.DefaultName, new JwtOptions
        {
            Issuer = "",
            Audience = "",
            Key = "quickbite-super-secret-development-key-change-me"
        });

        Assert.False(result.Succeeded);
        Assert.Contains(nameof(JwtOptions.Issuer), result.FailureMessage);
        Assert.Contains(nameof(JwtOptions.Audience), result.FailureMessage);
    }
}
