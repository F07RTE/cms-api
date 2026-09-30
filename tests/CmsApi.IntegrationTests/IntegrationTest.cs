// Every test shares the one compose test database, so tests run one at a time.
[assembly: NonParallelizable]

namespace CmsApi.IntegrationTests;

public abstract class IntegrationTest
{
    [SetUp]
    public async Task ResetAsync()
    {
        Orchestrator.ClearLogs();
        await Orchestrator.ResetDatabaseAsync();
        await Orchestrator.PurgeQueuesAsync();
    }
}

[SetUpFixture]
public sealed class DatabaseSetUp
{
    [OneTimeSetUp]
    public Task MigrateDatabaseAsync() => Orchestrator.MigrateDatabaseAsync();

    [OneTimeTearDown]
    public ValueTask DisposeOrchestratorAsync() => Orchestrator.DisposeAsync();
}
