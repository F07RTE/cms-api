using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CmsApi.Core.Domain.Events;
using CmsApi.Core.Domain.Users;
using FluentAssertions;

namespace CmsApi.IntegrationTests.UseCases;

public sealed class IngestionFlowTests : IntegrationTest
{
    [Test]
    public async Task CmsClient_WithPublishEventLaterDisabled()
    {
        var reader = await Orchestrator.CreateUserAsync(Orchestrator.ReaderUsername, UserRole.User);
        var admin = await Orchestrator.CreateUserAsync(Orchestrator.AdminUsername, UserRole.Admin);

        var posted = await Orchestrator.PostBatchAsync([PublishEvent]);
        posted.StatusCode.Should().Be(HttpStatusCode.Accepted);
        await Orchestrator.DrainInboxAsync();

        var published = await Orchestrator.GetContentEntityAsync(ContentEntityId, reader);
        published.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await published.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("version").GetInt64().Should().Be(Version);
        body.GetProperty("payload").GetProperty("title").GetString().Should().Be(Title);

        var disabled = await Orchestrator.DisableContentEntityAsync(ContentEntityId, admin);
        disabled.StatusCode.Should().Be(HttpStatusCode.OK);

        var hidden = await Orchestrator.GetContentEntityAsync(ContentEntityId, reader);
        await hidden.ShouldBeProblemAsync(HttpStatusCode.NotFound);

        (await Orchestrator.ReadEventLogAsync())
            .Should()
            .ContainSingle()
            .Which.Outcome.Should()
            .Be(EventOutcome.Applied);
        var stored = (await Orchestrator.ReadContentEntitiesAsync()).Single();
        stored.IsDisabledByAdmin.Should().BeTrue();
        stored.DisabledBy.Should().Be(Orchestrator.AdminUsername);
    }

    private const string ContentEntityId = "article-1";
    private const long Version = 1;
    private const string Title = "Hello";

    private static readonly DateTimeOffset PublishedAt = new(2026, 9, 29, 10, 0, 0, TimeSpan.Zero);

    private static readonly object PublishEvent = Orchestrator.CmsEvent(
        "publish",
        ContentEntityId,
        Version,
        PublishedAt,
        new { title = Title }
    );
}
