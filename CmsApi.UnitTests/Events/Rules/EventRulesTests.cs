using CmsApi.Core.Events;
using CmsApi.Core.Events.Rules;
using FluentAssertions;

namespace CmsApi.UnitTests.Events.Rules;

public sealed class EventRulesTests
{
    [Test]
    public void Publish_OfUnknownContentEntity()
    {
        var decision = EventRules.Decide(stored: null, Event(CmsEventType.Publish, 1, Later));

        decision.Should().Be(Applied(new ContentEntityState(1, NewPayload, true, Later)));
    }

    [Test]
    public void UnPublish_OfUnknownContentEntity()
    {
        var decision = EventRules.Decide(stored: null, Event(CmsEventType.UnPublish, 1, Later));

        decision.Should().Be(Applied(new ContentEntityState(1, NewPayload, false, Later)));
    }

    [Test]
    public void Publish_WithHigherVersion()
    {
        var decision = EventRules.Decide(StoredV2, Event(CmsEventType.Publish, 3, Earlier));

        decision.Should().Be(Applied(new ContentEntityState(3, NewPayload, true, Earlier)));
    }

    [Test]
    public void UnPublish_WithEqualVersionAndNewerTimestamp()
    {
        var decision = EventRules.Decide(StoredV2, Event(CmsEventType.UnPublish, 2, Later));

        decision.Should().Be(Applied(new ContentEntityState(2, NewPayload, false, Later)));
    }

    [TestCase(-1)]
    [TestCase(0)]
    public void Publish_WithEqualVersionAndNoNewerTimestamp(int secondsFromStored)
    {
        var timestamp = Stored.AddSeconds(secondsFromStored);

        var decision = EventRules.Decide(StoredV2, Event(CmsEventType.Publish, 2, timestamp));

        ShouldSkip(decision, EventOutcome.SkippedDuplicate);
    }

    [Test]
    public void UnPublish_WithLowerVersion()
    {
        var decision = EventRules.Decide(StoredV2, Event(CmsEventType.UnPublish, 1, Later));

        ShouldSkip(decision, EventOutcome.SkippedStale);
    }

    [Test]
    public void Batch_WithPublishThenUnPublishOfEqualVersion()
    {
        var publish = Event(CmsEventType.Publish, 2, Earlier);
        var unPublish = Event(CmsEventType.UnPublish, 2, Later) with { Payload = LaterPayload };

        var group = EventRules.DecideGroup(stored: null, [publish, unPublish]);

        group
            .DecidedEvents.Select(decided => decided.Decision.Outcome)
            .Should()
            .Equal(EventOutcome.Applied, EventOutcome.Applied);
        group.FinalState.Should().Be(new ContentEntityState(2, LaterPayload, false, Later));
    }

    // Placeholder until Deletes are final (ticket 04).
    [Test]
    public void Delete_OfAnyContentEntity()
    {
        var delete = new CmsEvent("article-1", CmsEventType.Delete, null, Later, null);

        var decision = EventRules.Decide(StoredV2, delete);

        ShouldSkip(decision, EventOutcome.Failed);
    }

    private const string StoredPayload = """{"title":"Stored"}""";
    private const string NewPayload = """{"title":"New"}""";
    private const string LaterPayload = """{"title":"Later"}""";

    private static readonly DateTimeOffset Stored = new(2026, 9, 29, 12, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Earlier = Stored.AddMinutes(-1);
    private static readonly DateTimeOffset Later = Stored.AddMinutes(1);

    private static readonly ContentEntityState StoredV2 = new(2, StoredPayload, true, Stored);

    private static CmsEvent Event(CmsEventType type, long version, DateTimeOffset timestamp) =>
        new("article-1", type, version, timestamp, NewPayload);

    private static EventDecision Applied(ContentEntityState newState) =>
        new(EventOutcome.Applied, Reason: null, newState);

    private static void ShouldSkip(EventDecision decision, EventOutcome outcome)
    {
        decision.Outcome.Should().Be(outcome);
        decision.Reason.Should().NotBeNullOrWhiteSpace();
        decision.NewState.Should().Be(StoredV2);
    }
}
