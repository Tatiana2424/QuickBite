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
}
