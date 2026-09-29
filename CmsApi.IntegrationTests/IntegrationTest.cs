namespace CmsApi.IntegrationTests;

public abstract class IntegrationTest
{
    [SetUp]
    public Task ResetAsync()
    {
        Orchestrator.ClearLogs();
        return Orchestrator.ResetDatabaseAsync();
    }
}
