using FluentAssertions;

namespace CmsApi.IntegrationTests.Worker;

public sealed class LeaderLockTests : IntegrationTest
{
    [Test]
    public async Task SecondReplica_WhileFirstIsLeader()
    {
        await using var first = Orchestrator.CreateLeaderLock();
        await using var second = Orchestrator.CreateLeaderLock();
        await first.TryAcquireAsync(CancellationToken.None);

        var acquired = await second.TryAcquireAsync(CancellationToken.None);

        acquired.Should().BeFalse();
        (await first.IsHeldAsync(CancellationToken.None)).Should().BeTrue();
    }

    [Test]
    public async Task SecondReplica_AfterFirstStepsDown()
    {
        await using var second = Orchestrator.CreateLeaderLock();
        await using (var first = Orchestrator.CreateLeaderLock())
        {
            await first.TryAcquireAsync(CancellationToken.None);
        }

        var acquired = await second.TryAcquireAsync(CancellationToken.None);

        acquired.Should().BeTrue();
    }
}
