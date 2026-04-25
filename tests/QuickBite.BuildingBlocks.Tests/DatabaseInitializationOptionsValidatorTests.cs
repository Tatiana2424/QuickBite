using Microsoft.Extensions.Options;
using QuickBite.BuildingBlocks.Common;

namespace QuickBite.BuildingBlocks.Tests;

public sealed class DatabaseInitializationOptionsValidatorTests
{
    private readonly DatabaseInitializationOptionsValidator _validator = new();

    [Fact]
    public void Validate_succeeds_for_default_options()
    {
        var result = _validator.Validate(Options.DefaultName, new DatabaseInitializationOptions());

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void Validate_fails_when_retry_count_is_not_positive()
    {
        var result = _validator.Validate(Options.DefaultName, new DatabaseInitializationOptions
        {
            MigrationRetryCount = 0
        });

        Assert.False(result.Succeeded);
        Assert.Contains("MigrationRetryCount", result.FailureMessage);
    }

    [Fact]
    public void Validate_fails_when_retry_delay_is_negative()
    {
        var result = _validator.Validate(Options.DefaultName, new DatabaseInitializationOptions
        {
            MigrationRetryDelaySeconds = -1
        });

        Assert.False(result.Succeeded);
        Assert.Contains("MigrationRetryDelaySeconds", result.FailureMessage);
    }
}
