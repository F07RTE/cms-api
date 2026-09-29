using System.Net;
using System.Net.Http.Json;
using System.Net.Mime;
using System.Text;
using System.Text.Json;
using CmsApi.Core.Batches;
using CmsApi.Core.Inbox;
using FluentAssertions;

namespace CmsApi.IntegrationTests.Api.Cms.Events;

public sealed class PostTests : IntegrationTest
{
    [Test]
    public async Task CmsClient_WithValidBatch()
    {
        var response = await Orchestrator.PostBatchAsync(ValidBatch);

        response.StatusCode.Should().Be(HttpStatusCode.Accepted);

        var stored = (await Orchestrator.ReadInboxAsync()).Should().ContainSingle().Subject;
        stored.Body.Should().Be(ValidBatch);
        stored.EventCount.Should().Be(ValidBatchEventCount);
        stored.Status.Should().Be(InboxStatus.Pending);
        stored.Attempts.Should().Be(0);
        stored.ReceivedAt.Should().Be(Orchestrator.Clock.GetUtcNow());
        stored.NextAttemptAt.Should().Be(stored.ReceivedAt);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("batchId").GetInt64().Should().Be(stored.Id);
        body.GetProperty("eventCount").GetInt32().Should().Be(ValidBatchEventCount);
        body.GetProperty("receivedAt").GetDateTimeOffset().Should().Be(stored.ReceivedAt);
    }

    [Test]
    public async Task CmsClient_WithEmptyArray()
    {
        var response = await Orchestrator.PostBatchAsync("[]");

        await ShouldBeRejectedAsync(response, HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task CmsClient_WithNonArrayBody()
    {
        var response = await Orchestrator.PostBatchAsync("""{ "type": "publish" }""");

        await ShouldBeRejectedAsync(response, HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task CmsClient_With1001Events()
    {
        var events = Enumerable.Repeat(new { type = "delete" }, BatchLimits.MaxEvents + 1);

        var response = await Orchestrator.PostBatchAsync(events);

        await ShouldBeRejectedAsync(response, HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task CmsClient_WithOversizeBody()
    {
        var oversize = $"[{new string(' ', BatchLimits.MaxBodyBytes)}]";

        var response = await Orchestrator.PostBatchAsync(oversize);

        await ShouldBeRejectedAsync(response, HttpStatusCode.RequestEntityTooLarge);
    }

    [Test]
    public async Task CmsClient_WithInvalidUtf8Body()
    {
        byte[] invalidUtf8 = [.. "[\""u8, InvalidUtf8Byte, .. "\"]"u8];

        var response = await Orchestrator.PostBatchAsync(
            invalidUtf8,
            Orchestrator.CmsClientCredentials()
        );

        await ShouldBeRejectedAsync(response, HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task AnonymousUser_WithoutCredentials()
    {
        var response = await Orchestrator.PostBatchAsync(
            Encoding.UTF8.GetBytes(ValidBatch),
            credentials: null
        );

        await ShouldBeRejectedAsync(response, HttpStatusCode.Unauthorized);
        response
            .Headers.WwwAuthenticate.Should()
            .ContainSingle()
            .Which.ToString()
            .Should()
            .Be("Basic realm=\"cms-api\", charset=\"UTF-8\"");
    }

    // A lone continuation byte: never valid UTF-8.
    private const byte InvalidUtf8Byte = 0x80;
    private const int ValidBatchEventCount = 2;

    // Irregular whitespace on purpose: the Inbox keeps the original text, byte for byte.
    private const string ValidBatch = """
        [ {"type": "publish", "id": "article-1", "version": 1,
           "timestamp": "2026-09-29T12:00:00Z", "payload": {"title": "Hello"}},
          {"type":"delete","id":"article-2","timestamp":"2026-09-29T12:01:00Z"} ]
        """;

    private static async Task ShouldBeRejectedAsync(
        HttpResponseMessage response,
        HttpStatusCode status
    )
    {
        await response.ShouldBeProblemAsync(status);
        (await Orchestrator.ReadInboxAsync()).Should().BeEmpty();
    }
}
