namespace CmsApi.IntegrationTests;

public abstract class IntegrationTest
{
    [SetUp]
    public Task ResetDatabaseAsync() => Orchestrator.ResetDatabaseAsync();
}
