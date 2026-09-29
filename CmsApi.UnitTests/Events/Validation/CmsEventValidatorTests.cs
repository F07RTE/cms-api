using System.Text.Json;
using CmsApi.Core.Events;
using CmsApi.Core.Events.Validation;
using FluentAssertions;

namespace CmsApi.UnitTests.Events.Validation;

public sealed class CmsEventValidatorTests
{
    [Test]
    public void Event_WithValidPublish()
    {
        var result = Validate(
            """
            {"type":"publish","id":"article-1","version":2,
             "timestamp":"2026-09-29T14:00:00+02:00","payload":{"title":"Hello"},"unknown":true}
            """
        );

        var cmsEvent = result.Should().BeOfType<ValidCmsEvent>().Which.Event;
        cmsEvent
            .Should()
            .Be(
                new CmsEvent("article-1", CmsEventType.Publish, 2, NoonUtc, """{"title":"Hello"}""")
            );
        cmsEvent.Timestamp.Offset.Should().Be(TimeSpan.Zero);
    }

    [Test]
    public void Event_WithValidUnPublish()
    {
        var result = Validate(
            """{"type":"unPublish","id":"article-1","version":1,"timestamp":"2026-09-29T12:00:00Z","payload":{}}"""
        );

        result
            .Should()
            .BeOfType<ValidCmsEvent>()
            .Which.Event.Should()
            .Be(new CmsEvent("article-1", CmsEventType.UnPublish, 1, NoonUtc, "{}"));
    }

    [Test]
    public void Event_WithDeleteCarryingVersionAndPayload()
    {
        var result = Validate(
            """{"type":"delete","id":"article-1","version":"x","payload":7,"timestamp":"2026-09-29T12:00:00Z"}"""
        );

        result
            .Should()
            .BeOfType<ValidCmsEvent>()
            .Which.Event.Should()
            .Be(new CmsEvent("article-1", CmsEventType.Delete, null, NoonUtc, null));
    }

    [TestCaseSource(nameof(WithoutUsableId))]
    public void Event_WithoutUsableId(string json) => ShouldFail(json, expectedId: null);

    [TestCaseSource(nameof(WithInvalidField))]
    public void Event_WithInvalidField(string json) => ShouldFail(json, expectedId: "article-1");

    private const string ValidTimestamp = "2026-09-29T12:00:00Z";

    private static readonly DateTimeOffset NoonUtc = new(2026, 9, 29, 12, 0, 0, TimeSpan.Zero);

    private static IEnumerable<TestCaseData> WithoutUsableId()
    {
        yield return Case("not an object", "\"article-1\"");
        yield return Case("missing id", Publish(id: null));
        yield return Case("id not a string", Publish(id: "7"));
        yield return Case("empty id", Publish(id: "\"\""));
        yield return Case(
            "id too long",
            Publish(id: $"\"{new string('a', CmsEventLimits.MaxIdLength + 1)}\"")
        );
        yield return Case("control char in id", Publish(id: "\"article\\u0007\""));
        yield return Case("leading whitespace in id", Publish(id: "\" article-1\""));
        yield return Case("trailing whitespace in id", Publish(id: "\"article-1 \""));
    }

    private static IEnumerable<TestCaseData> WithInvalidField()
    {
        yield return Case("wrong casing of type", Publish(type: "\"Publish\""));
        yield return Case("unknown type", Publish(type: "\"archive\""));
        yield return Case("missing type", Publish(type: null));
        yield return Case("missing timestamp", Publish(timestamp: null));
        yield return Case(
            "timestamp without offset",
            Publish(timestamp: "\"2026-09-29T12:00:00\"")
        );
        yield return Case("timestamp not a date", Publish(timestamp: "\"yesterday\""));
        yield return Case("missing version", Publish(version: null));
        yield return Case("version below 1", Publish(version: "0"));
        yield return Case("version not an integer", Publish(version: "1.5"));
        yield return Case("version as a string", Publish(version: "\"1\""));
        yield return Case("missing payload", Publish(payload: null));
        yield return Case("payload not an object", Publish(payload: "[]"));
        yield return Case("null char in payload value", Publish(payload: """{"a":"x\u0000"}"""));
        yield return Case("null char in payload key", Publish(payload: """{"\u0000":1}"""));
        yield return Case(
            "payload too large",
            Publish(payload: $$"""{"a":"{{new string('x', CmsEventLimits.MaxPayloadBytes)}}"}""")
        );
        yield return Case("duplicate key", $$"""{"type":"delete",{{Publish()[1..]}}""");
        yield return Case("duplicate key in payload", Publish(payload: """{"a":1,"a":2}"""));
    }

    // Arguments are raw JSON values; null leaves the property out.
    private static string Publish(
        string? type = "\"publish\"",
        string? id = "\"article-1\"",
        string? version = "1",
        string? timestamp = $"\"{ValidTimestamp}\"",
        string? payload = "{}"
    )
    {
        var properties = new (string Name, string? Value)[]
        {
            ("type", type),
            ("id", id),
            ("version", version),
            ("timestamp", timestamp),
            ("payload", payload),
        };
        var present = properties
            .Where(property => property.Value is not null)
            .Select(property => $"\"{property.Name}\":{property.Value}");
        return $"{{{string.Join(',', present)}}}";
    }

    private static TestCaseData Case(string name, string json) =>
        new TestCaseData(json).SetName($"{{m}}({name})");

    private static CmsEventValidation Validate(string json)
    {
        using var document = JsonDocument.Parse(json);
        return CmsEventValidator.Validate(document.RootElement);
    }

    private static void ShouldFail(string json, string? expectedId)
    {
        var failed = Validate(json).Should().BeOfType<FailedCmsEvent>().Subject;

        failed.Id.Should().Be(expectedId);
        failed.RawEvent.Should().Be(json);
        failed.Reason.Should().NotBeNullOrWhiteSpace();
    }
}
