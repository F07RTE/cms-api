using CmsApi.Core.Domain.Events;
using CmsApi.Core.Domain.Inbox;
using CmsApi.Data;
using CmsApi.Data.Inbox;
using CmsApi.Worker;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace CmsApi.IntegrationTests.Worker;

/// <summary>Smoke test: the real Worker host, polling, picks up a posted Batch.</summary>
public sealed class WorkerHostTests : IntegrationTest
{
    [Test]
    public async Task CmsClient_WithPostedBatch()
    {
        using var host = BuildWorkerHost();
        await host.StartAsync();

        await Orchestrator.PostBatchAsync([
            Orchestrator.CmsEvent(
                "publish",
                "article-1",
                1,
                Orchestrator.Clock.GetUtcNow(),
                Payload
            ),
        ]);
        await MakeBatchesDueOnRealClockAsync();
        var batch = await WaitUntilDoneAsync();
        await host.StopAsync();

        batch.Status.Should().Be(InboxStatus.Done);
        (await Orchestrator.ReadContentEntitiesAsync())
            .Should()
            .ContainSingle()
            .Which.Id.Should()
            .Be("article-1");
    }

    private const string TestingEnvironment = "Testing";
    private const string SolutionFile = "CmsApi.slnx";
    private const string SourceDirectory = "src";
    private const string WorkerProjectDirectory = "CmsApi.Worker";

    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan PollDelay = TimeSpan.FromMilliseconds(100);

    private static readonly object Payload = new { title = "Hello" };

    // The real host on the real clock, so it idles and polls as in production.
    private static IHost BuildWorkerHost() =>
        WorkerHost
            .Configure(
                Host.CreateApplicationBuilder(
                    new HostApplicationBuilderSettings
                    {
                        EnvironmentName = TestingEnvironment,
                        ContentRootPath = WorkerContentRoot(),
                    }
                )
            )
            .Build();

    // The API host received the Batch on the fake clock, which may run ahead of the real one.
    private static async Task MakeBatchesDueOnRealClockAsync()
    {
        await using var scope = Orchestrator.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<WriteDbContext>();
        var now = TimeProvider.System.GetUtcNow();
        await context.InboxBatches.ExecuteUpdateAsync(setters =>
            setters.SetProperty(batch => batch.NextAttemptAt, now)
        );
    }

    private static async Task<InboxBatch> WaitUntilDoneAsync()
    {
        using var timeout = new CancellationTokenSource(Timeout);
        while (true)
        {
            var batch = (await Orchestrator.ReadInboxAsync()).Single();
            if (batch.Status == InboxStatus.Done)
            {
                return batch;
            }

            await Task.Delay(PollDelay, timeout.Token);
        }
    }

    // The Worker's appsettings live beside its project, not in the test output.
    private static string WorkerContentRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (!File.Exists(Path.Combine(directory.FullName, SolutionFile)))
        {
            directory =
                directory.Parent
                ?? throw new InvalidOperationException($"{SolutionFile} not found.");
        }

        return Path.Combine(directory.FullName, SourceDirectory, WorkerProjectDirectory);
    }
}
