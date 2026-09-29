using CmsApi.Core.Events;
using CmsApi.Core.Events.Rules;
using FluentAssertions;

namespace CmsApi.UnitTests.Events.Rules;

public sealed class CmsEventOrderingTests
{
    [Test]
    public void Events_WithSeveralIds()
    {
        var first = Publish("article-1", version: 1, second: 0);
        var other = Publish("article-2", version: 1, second: 1);
        var second = Publish("article-1", version: 2, second: 2);

        var groups = CmsEventOrdering.GroupById([first, other, second]);

        groups
            .Should()
            .BeEquivalentTo(
                [
                    new ContentEntityGroup("article-1", [first, second]),
                    new ContentEntityGroup("article-2", [other]),
                ],
                options => options.WithStrictOrdering()
            );
    }

    [Test]
    public void Events_WithTimestampsOutOfOrder()
    {
        var later = Publish("article-1", version: 1, second: 5);
        var earlier = Publish("article-1", version: 2, second: 0);

        var group = CmsEventOrdering.GroupById([later, earlier]).Should().ContainSingle().Subject;

        group.Events.Should().Equal(earlier, later);
    }

    [Test]
    public void Events_WithEqualTimestamps()
    {
        var higher = Publish("article-1", version: 3, second: 0);
        var lower = Publish("article-1", version: 2, second: 0);

        var group = CmsEventOrdering.GroupById([higher, lower]).Should().ContainSingle().Subject;

        group.Events.Should().Equal(lower, higher);
    }

    private static readonly DateTimeOffset Noon = new(2026, 9, 29, 12, 0, 0, TimeSpan.Zero);

    private static CmsEvent Publish(string id, long version, int second) =>
        new(id, CmsEventType.Publish, version, Noon.AddSeconds(second), "{}");
}
