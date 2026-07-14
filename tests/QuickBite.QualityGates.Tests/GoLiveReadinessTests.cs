namespace QuickBite.QualityGates.Tests;

public sealed class GoLiveReadinessTests
{
    [Fact]
    public void Go_live_readiness_document_covers_operational_launch_gates()
    {
        var readiness = Read("docs", "go-live-readiness.md");

        Assert.Contains("Backup", readiness, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Kafka", readiness, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("RTO", readiness, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("RPO", readiness, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("load", readiness, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("soak", readiness, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("security", readiness, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("SLI", readiness, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("SLO", readiness, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("on-call", readiness, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("approval", readiness, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Backup_and_dr_runbook_covers_all_service_databases_and_kafka_replay()
    {
        var runbook = Read("docs", "operations", "backup-disaster-recovery.md");

        foreach (var database in new[]
        {
            "QuickBiteIdentityDb",
            "QuickBiteCatalogDb",
            "QuickBiteOrdersDb",
            "QuickBitePaymentsDb",
            "QuickBiteDeliveryDb"
        })
        {
            Assert.Contains(database, runbook, StringComparison.Ordinal);
        }

        Assert.Contains("Restore Rehearsal", runbook, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Recovery Objectives", runbook, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Kafka Retention", runbook, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Replay Checklist", runbook, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("dead-letter", runbook, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Incident_runbooks_cover_common_go_live_failure_modes()
    {
        var runbook = Read("docs", "operations", "incident-runbooks.md");

        Assert.Contains("Gateway High 5xx", runbook, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Database Readiness Failure", runbook, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Kafka Consumer Lag", runbook, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Outbox Backlog", runbook, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Security Incident", runbook, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Post-Incident Review", runbook, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Launch_and_rollback_checklists_cover_release_data_validation_and_rollback()
    {
        var checklist = Read("docs", "operations", "launch-rollback-checklists.md");

        Assert.Contains("Launch Checklist", checklist, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Rollback Checklist", checklist, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("migration", checklist, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("restore rehearsal", checklist, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("load test", checklist, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("soak test", checklist, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("TLS certificate", checklist, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("last known-good", checklist, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Approval", checklist, StringComparison.OrdinalIgnoreCase);
    }

    private static string Read(params string[] segments)
    {
        return File.ReadAllText(RepositoryPaths.File(segments));
    }
}
