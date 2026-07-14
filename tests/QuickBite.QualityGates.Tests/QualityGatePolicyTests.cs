namespace QuickBite.QualityGates.Tests;

public sealed class QualityGatePolicyTests
{
    [Fact]
    public void Repository_has_ci_workflow_for_backend_frontend_and_compose_validation()
    {
        var workflow = File.ReadAllText(RepositoryPaths.File(".github", "workflows", "quality-gates.yml"));

        Assert.Contains("dotnet test", workflow, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("npm ci", workflow, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("npm test", workflow, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("npm run build", workflow, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("docker compose", workflow, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("XPlat Code Coverage", workflow, StringComparison.Ordinal);
    }

    [Fact]
    public void Repository_has_release_workflow_for_images_migrations_scanning_and_promotion()
    {
        var workflow = File.ReadAllText(RepositoryPaths.File(".github", "workflows", "release.yml"));

        Assert.Contains("docker/build-push-action", workflow, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("ghcr.io", workflow, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("dotnet ef migrations bundle", workflow, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("aquasecurity/trivy-action", workflow, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("dependency-scan", workflow, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("environment:", workflow, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("staging", workflow, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("production", workflow, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("type=sha", workflow, StringComparison.OrdinalIgnoreCase);
    }
}
