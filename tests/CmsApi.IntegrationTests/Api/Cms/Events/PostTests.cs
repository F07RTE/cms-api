using System.Net;
using System.Net.Http.Json;
using System.Net.Mime;
using System.Text;
using System.Text.Json;
using CmsApi.Core.Domain.Batches;
using CmsApi.Core.Domain.Inbox;
using CmsApi.Core.Domain.Users;
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

        (await Orchestrator.TakeQueuedBatchIdsAsync()).Should().Equal(stored.Id);
    }

    [Test]
    public async Task CmsClient_WithNonArrayBody()
    {
        var response = await Orchestrator.PostBatchAsync("""{ "type": "publish" }""");

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
    public async Task AnonymousUser_WithoutCredentials()
    {
        var response = await Orchestrator.PostBatchAsync(
            Encoding.UTF8.GetBytes(ValidBatch),
            credentials: null
        );

        await response.ShouldBeChallengedAsync();
        (await Orchestrator.ReadInboxAsync()).Should().BeEmpty();
    }

    [Test]
    public async Task CmsClient_WithWrongPassword()
    {
        var wrongPassword = Orchestrator.CmsClientCredentials() with
        {
            Password = Guid.NewGuid().ToString(),
        };

        var response = await Orchestrator.PostBatchAsync(
            Encoding.UTF8.GetBytes(ValidBatch),
            wrongPassword
        );

        await response.ShouldBeChallengedAsync();
        (await Orchestrator.ReadInboxAsync()).Should().BeEmpty();
    }

    [TestCase(UserRole.User)]
    [TestCase(UserRole.Admin)]
    public async Task User_WithValidBatch(UserRole role)
    {
        var user = await Orchestrator.CreateUserAsync(Orchestrator.ReaderUsername, role);

        var response = await Orchestrator.PostBatchAsync(Encoding.UTF8.GetBytes(ValidBatch), user);

        await ShouldBeRejectedAsync(response, HttpStatusCode.Forbidden);
    }

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
