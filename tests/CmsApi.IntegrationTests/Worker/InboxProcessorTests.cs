using System.Text.Json;
using System.Text.Json.Nodes;
using CmsApi.Core.Domain.Events;
using CmsApi.Core.Domain.Inbox;
using CmsApi.Core.UseCases.ProcessBatch;
using CmsApi.Core.UseCases.ProcessInbox;
using CmsApi.Data.ContentEntities;
using CmsApi.Data.EventLog;
using CmsApi.Data.Tombstones;
using FluentAssertions;
using Microsoft.Extensions.Logging;

namespace CmsApi.IntegrationTests.Worker;

public sealed class InboxProcessorTests : IntegrationTest
{
    [Test]
    public async Task CmsClient_WithPublishEvent()
    {
        await Orchestrator.PostBatchAsync([
            Orchestrator.CmsEvent("publish", "article-1", 1, T1, HelloPayload),
        ]);

        await Orchestrator.DrainInboxAsync();

        var stored = (await Orchestrator.ReadContentEntitiesAsync())
            .Should()
            .ContainSingle()
            .Subject;
        stored.Id.Should().Be("article-1");
        stored.Version.Should().Be(1);
        stored.IsPublished.Should().BeTrue();
        stored.LastEventAt.Should().Be(T1);
        ShouldBeSameJson(stored.Payload, JsonSerializer.Serialize(HelloPayload));

        var batch = (await Orchestrator.ReadInboxAsync()).Should().ContainSingle().Subject;
        batch.Status.Should().Be(InboxStatus.Done);
        batch.Attempts.Should().Be(1);
        batch.ProcessedAt.Should().Be(Orchestrator.Clock.GetUtcNow());

        var entry = (await Orchestrator.ReadEventLogAsync()).Should().ContainSingle().Subject;
        entry
            .Should()
            .BeEquivalentTo(
                new EventLogEntry
                {
                    Id = entry.Id,
                    BatchId = batch.Id,
                    ContentEntityId = "article-1",
                    EventType = CmsEventType.Publish,
                    Version = 1,
                    EventTimestamp = T1,
                    Outcome = EventOutcome.Applied,
                    Reason = null,
                    RawEvent = null,
                    ProcessedAt = Orchestrator.Clock.GetUtcNow(),
                }
            );
    }

    [Test]
    public async Task CmsClient_WithOneInvalidEvent()
    {
        var invalid = Orchestrator.CmsEvent("publish", "article-1", version: 0, T1, HelloPayload);
        await Orchestrator.PostBatchAsync([
            invalid,
            Orchestrator.CmsEvent("publish", "article-2", 1, T1, HelloPayload),
        ]);

        await Orchestrator.DrainInboxAsync();

        (await Orchestrator.ReadContentEntitiesAsync())
            .Should()
            .ContainSingle()
            .Which.Id.Should()
            .Be("article-2");
        var eventLog = await Orchestrator.ReadEventLogAsync();
        eventLog.Should().HaveCount(2);
        var failed = eventLog
            .Should()
            .ContainSingle(entry => entry.Outcome == EventOutcome.Failed)
            .Subject;
        failed.ContentEntityId.Should().Be("article-1");
        failed.RawEvent.Should().Be(JsonSerializer.Serialize(invalid));
        failed.Reason.Should().NotBeNullOrWhiteSpace();
        eventLog
            .Should()
            .ContainSingle(entry => entry.Outcome == EventOutcome.Applied)
            .Which.ContentEntityId.Should()
            .Be("article-2");
        var batch = (await Orchestrator.ReadInboxAsync()).Single();
        batch.Status.Should().Be(InboxStatus.Done);

        var recorded = Orchestrator.ReadLogsForBatch(batch.Id);
        recorded.Should().HaveCount(2);
        recorded
            .Should()
            .ContainSingle(log => log.Level == LogLevel.Warning)
            .Which.Properties.Should()
            .Contain(RecordedLog.ContentEntityId, "article-1")
            .And.Contain(RecordedLog.Outcome, EventOutcome.Failed)
            .And.Contain(RecordedLog.Reason, failed.Reason);
        recorded
            .Should()
            .ContainSingle(log => log.Level == LogLevel.Information)
            .Which.Properties.Should()
            .Contain(RecordedLog.ContentEntityId, "article-2")
            .And.Contain(RecordedLog.Outcome, EventOutcome.Applied);
    }

    [Test]
    public async Task CmsClient_WithNewerVersionOfDisabledContentEntity()
    {
        var seeded = Seeded("article-1", version: 1);
        seeded.IsDisabledByAdmin = true;
        seeded.DisabledAt = T1;
        seeded.DisabledBy = "admin";
        await Orchestrator.SeedEntityAsync(seeded);
        await Orchestrator.PostBatchAsync([
            Orchestrator.CmsEvent("publish", "article-1", 2, T2, HelloPayload),
        ]);

        await Orchestrator.DrainInboxAsync();

        var stored = (await Orchestrator.ReadContentEntitiesAsync())
            .Should()
            .ContainSingle()
            .Subject;
        stored.Version.Should().Be(2);
        stored.IsDisabledByAdmin.Should().BeTrue();
        stored.DisabledAt.Should().Be(T1);
        stored.DisabledBy.Should().Be("admin");
        (await Orchestrator.ReadEventLogAsync())
            .Should()
            .ContainSingle()
            .Which.Outcome.Should()
            .Be(EventOutcome.Applied);
    }

    [Test]
    public async Task CmsClient_WithReplayedBatchHoldingInvalidEvent()
    {
        await Orchestrator.PostBatchAsync([
            Orchestrator.CmsEvent("publish", "article-1", version: 0, T1, HelloPayload),
            Orchestrator.CmsEvent("publish", "article-2", 1, T1, HelloPayload),
        ]);
        await Orchestrator.DrainInboxAsync();
        await Orchestrator.RequeueBatchesAsync();

        await Orchestrator.DrainInboxAsync();

        var eventLog = await Orchestrator.ReadEventLogAsync();
        eventLog.Where(entry => entry.Outcome == EventOutcome.Failed).Should().ContainSingle();
        eventLog
            .Where(entry => entry.ContentEntityId == "article-2")
            .Select(entry => entry.Outcome)
            .Should()
            .Equal(EventOutcome.Applied, EventOutcome.SkippedDuplicate);

        var batchId = (await Orchestrator.ReadInboxAsync()).Single().Id;
        Orchestrator
            .ReadLogsForBatch(batchId)
            .Where(log => log.Level == LogLevel.Warning)
            .Should()
            .ContainSingle()
            .Which.Properties.Should()
            .Contain(RecordedLog.Outcome, EventOutcome.Failed);
    }

    [Test]
    public async Task CmsClient_WithDeleteThenLaterPublish()
    {
        await Orchestrator.SeedEntityAsync(Seeded("article-1", version: 1));
        await Orchestrator.PostBatchAsync([Orchestrator.DeleteEvent("article-1", T2)]);
        await Orchestrator.DrainInboxAsync();
        await Orchestrator.PostBatchAsync([
            Orchestrator.CmsEvent("publish", "article-1", 2, T3, HelloPayload),
        ]);

        await Orchestrator.DrainInboxAsync();

        (await Orchestrator.ReadContentEntitiesAsync()).Should().BeEmpty();
        (await Orchestrator.ReadTombstonesAsync())
            .Should()
            .ContainSingle()
            .Which.Should()
            .BeEquivalentTo(
                new Tombstone
                {
                    Id = "article-1",
                    DeletedAt = T2,
                    RecordedAt = Orchestrator.Clock.GetUtcNow(),
                }
            );
        var log = await Orchestrator.ReadEventLogAsync();
        log.Select(entry => (entry.EventType, entry.Outcome))
            .Should()
            .Equal(
                (CmsEventType.Delete, EventOutcome.Applied),
                (CmsEventType.Publish, EventOutcome.SkippedDeleted)
            );
        log[1].Reason.Should().NotBeNullOrWhiteSpace();
    }

    [Test]
    public async Task CmsClient_WithBatchOrphanedByCrashedWorker()
    {
        await Orchestrator.PostBatchAsync([
            Orchestrator.CmsEvent("publish", "article-1", 1, T1, HelloPayload),
        ]);
        await Orchestrator.OrphanNextBatchAsync();

        await Orchestrator.RecoverOrphansAsync();
        await Orchestrator.DrainInboxAsync();

        var batch = (await Orchestrator.ReadInboxAsync()).Should().ContainSingle().Subject;
        batch.Status.Should().Be(InboxStatus.Done);
        batch.Attempts.Should().Be(2);
        (await Orchestrator.ReadContentEntitiesAsync())
            .Should()
            .ContainSingle()
            .Which.Id.Should()
            .Be("article-1");
    }

    [Test]
    public async Task CmsClient_WithBatchFailingOnce()
    {
        await Orchestrator.PostBatchAsync([
            Orchestrator.CmsEvent("publish", "article-1", 1, T1, HelloPayload),
        ]);

        await Orchestrator.DrainInboxAsync(new ThrowingBatchProcessor());

        var batch = (await Orchestrator.ReadInboxAsync()).Should().ContainSingle().Subject;
        batch.Status.Should().Be(InboxStatus.Pending);
        batch.Attempts.Should().Be(1);
        batch
            .NextAttemptAt.Should()
            .Be(Orchestrator.Clock.GetUtcNow() + RetryBackoff.After(batch.Attempts));
        batch.LastError.Should().Contain(ThrowingBatchProcessor.Failure);
        (await Orchestrator.ReadContentEntitiesAsync()).Should().BeEmpty();
    }

    [Test]
    public async Task CmsClient_WithBatchFailingEveryAttempt()
    {
        await Orchestrator.PostBatchAsync([
            Orchestrator.CmsEvent("publish", "article-1", 1, T1, HelloPayload),
        ]);

        // One more round than MaxAttempts: a Dead Batch is never claimed again.
        for (var round = 0; round <= InboxProcessor.MaxAttempts; round++)
        {
            await Orchestrator.DrainInboxAsync(new ThrowingBatchProcessor());
            Orchestrator.Clock.Advance(RetryBackoff.MaxDelay);
        }

        var batch = (await Orchestrator.ReadInboxAsync()).Should().ContainSingle().Subject;
        batch.Status.Should().Be(InboxStatus.Dead);
        batch.Attempts.Should().Be(InboxProcessor.MaxAttempts);
        batch.LastError.Should().Contain(ThrowingBatchProcessor.Failure);
    }

    private static readonly DateTimeOffset T1 = new(2026, 9, 29, 11, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset T2 = T1.AddMinutes(1);
    private static readonly DateTimeOffset T3 = T2.AddMinutes(1);

    private static readonly object HelloPayload = new { title = "Hello" };

    private static ContentEntity Seeded(string id, long version) =>
        new()
        {
            Id = id,
            Version = version,
            Payload = """{"title":"Seeded"}""",
            IsPublished = true,
            LastEventAt = T1,
        };

    // jsonb keeps the meaning of the payload, not its bytes.
    private static void ShouldBeSameJson(string actual, string expected) =>
        JsonNode.DeepEquals(JsonNode.Parse(actual), JsonNode.Parse(expected)).Should().BeTrue();

    private sealed class ThrowingBatchProcessor : IBatchProcessor
    {
        public const string Failure = "database unreachable";

        public Task ProcessAsync(ClaimedBatch batch, CancellationToken cancellationToken) =>
            throw new InvalidOperationException(Failure);
    }
}
