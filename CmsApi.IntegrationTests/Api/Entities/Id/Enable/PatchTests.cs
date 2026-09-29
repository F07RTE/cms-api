using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CmsApi.Core.Auth;
using CmsApi.Core.Users;
using CmsApi.Data.ContentEntities;
using FluentAssertions;

namespace CmsApi.IntegrationTests.Api.Entities.Id.Enable;

public sealed class PatchTests : IntegrationTest
{
    [Test]
    public async Task AdminUser_WithDisabledContentEntity()
    {
        var admin = await Orchestrator.CreateUserAsync(Orchestrator.AdminUsername, UserRole.Admin);
        await Orchestrator.SeedEntityAsync(
            Orchestrator.Disable(Seeded(), Orchestrator.AdminUsername, DisabledAt)
        );

        var response = await EnableAsync(admin);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("id").GetString().Should().Be(ContentEntityId);
        body.GetProperty("isDisabledByAdmin").GetBoolean().Should().BeFalse();
        body.GetProperty("disabledAt").ValueKind.Should().Be(JsonValueKind.Null);
        body.GetProperty("disabledBy").ValueKind.Should().Be(JsonValueKind.Null);
        body.GetProperty("updatedAt").GetDateTimeOffset().Should().Be(LastEventAt);

        var stored = (await Orchestrator.ReadContentEntitiesAsync()).Single();
        stored.IsDisabledByAdmin.Should().BeFalse();
        stored.DisabledAt.Should().BeNull();
        stored.DisabledBy.Should().BeNull();
        stored.LastEventAt.Should().Be(LastEventAt);
    }

    [Test]
    public async Task AdminUser_WithEnabledContentEntity()
    {
        var admin = await Orchestrator.CreateUserAsync(Orchestrator.AdminUsername, UserRole.Admin);
        await Orchestrator.SeedEntityAsync(Seeded());

        var response = await EnableAsync(admin);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("isDisabledByAdmin").GetBoolean().Should().BeFalse();

        var stored = (await Orchestrator.ReadContentEntitiesAsync()).Single();
        stored.IsDisabledByAdmin.Should().BeFalse();
        stored.DisabledAt.Should().BeNull();
    }

    [Test]
    public async Task AdminUser_WithUnknownContentEntity()
    {
        var admin = await Orchestrator.CreateUserAsync(Orchestrator.AdminUsername, UserRole.Admin);

        var response = await EnableAsync(admin);

        await response.ShouldBeProblemAsync(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task DefaultUser_WithDisabledContentEntity()
    {
        var reader = await Orchestrator.CreateUserAsync(Orchestrator.ReaderUsername, UserRole.User);
        await Orchestrator.SeedEntityAsync(
            Orchestrator.Disable(Seeded(), Orchestrator.AdminUsername, DisabledAt)
        );

        var response = await EnableAsync(reader);

        await response.ShouldBeProblemAsync(HttpStatusCode.Forbidden);
        var stored = (await Orchestrator.ReadContentEntitiesAsync()).Single();
        stored.IsDisabledByAdmin.Should().BeTrue();
    }

    private const string ContentEntityId = "article-1";

    private static readonly DateTimeOffset LastEventAt = new(2026, 9, 29, 10, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset DisabledAt = new(2026, 9, 29, 11, 0, 0, TimeSpan.Zero);

    private static ContentEntity Seeded() =>
        Orchestrator.NewContentEntity(ContentEntityId, LastEventAt, isPublished: true);

    private static Task<HttpResponseMessage> EnableAsync(BasicCredentials credentials) =>
        Orchestrator.EnableContentEntityAsync(ContentEntityId, credentials);
}
