namespace CmsApi.IntegrationTests;

[SetUpFixture]
public sealed class DatabaseSetUp
{
    [OneTimeSetUp]
    public Task MigrateDatabaseAsync() => Orchestrator.MigrateDatabaseAsync();

    [OneTimeTearDown]
    public ValueTask DisposeOrchestratorAsync() => Orchestrator.DisposeAsync();
}
