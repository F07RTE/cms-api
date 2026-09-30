// Every test shares the one compose test database, so tests run one at a time.
[assembly: NonParallelizable]

namespace CmsApi.IntegrationTests;

/// <summary>The base of every integration test: a clean database and no logs from earlier tests.</summary>
public abstract class IntegrationTest
{
    [SetUp]
    public Task ResetAsync()
    {
        Orchestrator.ClearLogs();
        return Orchestrator.ResetDatabaseAsync();
    }
}

/// <summary>Migrates the test database once before any test, and stops the host after the last.</summary>
[SetUpFixture]
public sealed class DatabaseSetUp
{
    [OneTimeSetUp]
    public Task MigrateDatabaseAsync() => Orchestrator.MigrateDatabaseAsync();

    [OneTimeTearDown]
    public ValueTask DisposeOrchestratorAsync() => Orchestrator.DisposeAsync();
}
