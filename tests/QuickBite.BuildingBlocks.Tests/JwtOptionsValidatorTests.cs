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
            Key = "a-realistic-local-test-signing-key-value"
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
            Key = "a-realistic-local-test-signing-key-value"
        });

        Assert.False(result.Succeeded);
        Assert.Contains(nameof(JwtOptions.Issuer), result.FailureMessage);
        Assert.Contains(nameof(JwtOptions.Audience), result.FailureMessage);
    }

    [Fact]
    public void Validate_fails_when_development_key_is_used_without_opt_in()
    {
        var result = _validator.Validate(Options.DefaultName, new JwtOptions
        {
            Issuer = "QuickBite",
            Audience = "QuickBite.Web",
            Key = JwtOptions.DevelopmentSigningKey
        });

        Assert.False(result.Succeeded);
        Assert.Contains("development signing key", result.FailureMessage);
    }

    [Fact]
    public void Validate_allows_development_key_when_explicitly_enabled()
    {
        var result = _validator.Validate(Options.DefaultName, new JwtOptions
        {
            Issuer = "QuickBite",
            Audience = "QuickBite.Web",
            Key = JwtOptions.DevelopmentSigningKey,
            AllowDevelopmentSigningKey = true
        });

        Assert.True(result.Succeeded);
    }
}
