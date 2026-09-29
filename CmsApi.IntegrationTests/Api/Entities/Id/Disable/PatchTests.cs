using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CmsApi.Core.Auth;
using CmsApi.Core.Users;
using CmsApi.Data.ContentEntities;
using FluentAssertions;

namespace CmsApi.IntegrationTests.Api.Entities.Id.Disable;

public sealed class PatchTests : IntegrationTest
{
    [Test]
    public async Task AdminUser_WithEnabledContentEntity()
    {
        var admin = await Orchestrator.CreateUserAsync(Orchestrator.AdminUsername, UserRole.Admin);
        await Orchestrator.SeedEntityAsync(Seeded(isPublished: true));

        var response = await DisableAsync(admin);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("id").GetString().Should().Be(ContentEntityId);
        body.GetProperty("isDisabledByAdmin").GetBoolean().Should().BeTrue();
        body.GetProperty("disabledAt")
            .GetDateTimeOffset()
            .Should()
            .Be(Orchestrator.Clock.GetUtcNow());
        body.GetProperty("disabledBy").GetString().Should().Be(Orchestrator.AdminUsername);
        body.GetProperty("updatedAt").GetDateTimeOffset().Should().Be(LastEventAt);

        var stored = (await Orchestrator.ReadContentEntitiesAsync()).Single();
        stored.IsDisabledByAdmin.Should().BeTrue();
        stored.DisabledAt.Should().Be(Orchestrator.Clock.GetUtcNow());
        stored.DisabledBy.Should().Be(Orchestrator.AdminUsername);
        stored.LastEventAt.Should().Be(LastEventAt);
    }

    [Test]
    public async Task AdminUser_WithUnpublishedContentEntity()
    {
        var admin = await Orchestrator.CreateUserAsync(Orchestrator.AdminUsername, UserRole.Admin);
        await Orchestrator.SeedEntityAsync(Seeded(isPublished: false));

        var response = await DisableAsync(admin);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var stored = (await Orchestrator.ReadContentEntitiesAsync()).Single();
        stored.IsDisabledByAdmin.Should().BeTrue();
        stored.IsPublished.Should().BeFalse();
    }

    [Test]
    public async Task AdminUser_WithDisabledContentEntity()
    {
        var admin = await Orchestrator.CreateUserAsync(Orchestrator.AdminUsername, UserRole.Admin);
        await Orchestrator.SeedEntityAsync(
            Orchestrator.Disable(Seeded(isPublished: true), EarlierAdminUsername, EarlierDisabledAt)
        );

        var response = await DisableAsync(admin);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("disabledAt").GetDateTimeOffset().Should().Be(EarlierDisabledAt);
        body.GetProperty("disabledBy").GetString().Should().Be(EarlierAdminUsername);

        var stored = (await Orchestrator.ReadContentEntitiesAsync()).Single();
        stored.DisabledAt.Should().Be(EarlierDisabledAt);
        stored.DisabledBy.Should().Be(EarlierAdminUsername);
    }

    [Test]
    public async Task AdminUser_WithUnknownContentEntity()
    {
        var admin = await Orchestrator.CreateUserAsync(Orchestrator.AdminUsername, UserRole.Admin);

        var response = await DisableAsync(admin);

        await response.ShouldBeProblemAsync(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task AdminUser_WithDeletedContentEntity()
    {
        var admin = await Orchestrator.CreateUserAsync(Orchestrator.AdminUsername, UserRole.Admin);
        await Orchestrator.SeedEntityAsync(Seeded(isPublished: true));
        await Orchestrator.PostBatchAsync([Orchestrator.DeleteEvent(ContentEntityId, DeletedAt)]);
        await Orchestrator.DrainInboxAsync();

        var response = await DisableAsync(admin);

        await response.ShouldBeProblemAsync(HttpStatusCode.NotFound);
        (await Orchestrator.ReadContentEntitiesAsync()).Should().BeEmpty();
    }

    [Test]
    public async Task DefaultUser_WithEnabledContentEntity()
    {
        var reader = await Orchestrator.CreateUserAsync(Orchestrator.ReaderUsername, UserRole.User);
        await Orchestrator.SeedEntityAsync(Seeded(isPublished: true));

        var response = await DisableAsync(reader);

        await response.ShouldBeProblemAsync(HttpStatusCode.Forbidden);
        var stored = (await Orchestrator.ReadContentEntitiesAsync()).Single();
        stored.IsDisabledByAdmin.Should().BeFalse();
    }

    private const string ContentEntityId = "article-1";
    private const string EarlierAdminUsername = "earlier-admin";

    private static readonly DateTimeOffset LastEventAt = new(2026, 9, 29, 10, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset EarlierDisabledAt = new(
        2026,
        9,
        29,
        11,
        0,
        0,
        TimeSpan.Zero
    );
    private static readonly DateTimeOffset DeletedAt = new(2026, 9, 29, 11, 30, 0, TimeSpan.Zero);

    private static ContentEntity Seeded(bool isPublished) =>
        Orchestrator.NewContentEntity(ContentEntityId, LastEventAt, isPublished);

    private static Task<HttpResponseMessage> DisableAsync(BasicCredentials credentials) =>
        Orchestrator.DisableContentEntityAsync(ContentEntityId, credentials);
}
