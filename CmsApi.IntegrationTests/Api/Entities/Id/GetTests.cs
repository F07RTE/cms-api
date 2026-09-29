using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using CmsApi.Core.Users;
using CmsApi.Data.ContentEntities;
using FluentAssertions;

namespace CmsApi.IntegrationTests.Api.Entities.Id;

public sealed class GetTests : IntegrationTest
{
    [Test]
    public async Task DefaultUser_WithVisibleContentEntity()
    {
        var reader = await Orchestrator.CreateUserAsync(Orchestrator.ReaderUsername, UserRole.User);
        await Orchestrator.SeedEntityAsync(Seeded(isPublished: true));

        var response = await Orchestrator.GetContentEntityAsync(ContentEntityId, reader);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.PropertyNames().Should().BeEquivalentTo("id", "version", "payload", "updatedAt");
        body.GetProperty("id").GetString().Should().Be(ContentEntityId);
        body.GetProperty("version").GetInt64().Should().Be(Version);
        body.GetProperty("updatedAt").GetDateTimeOffset().Should().Be(LastEventAt);
        ShouldBeSameJson(body.GetProperty("payload"), Payload);
    }

    [Test]
    public async Task AdminUser_WithDisabledContentEntity()
    {
        var admin = await Orchestrator.CreateUserAsync(Orchestrator.AdminUsername, UserRole.Admin);
        await Orchestrator.SeedEntityAsync(
            Orchestrator.Disable(Seeded(isPublished: false), Orchestrator.AdminUsername, DisabledAt)
        );

        var response = await Orchestrator.GetContentEntityAsync(ContentEntityId, admin);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("id").GetString().Should().Be(ContentEntityId);
        body.GetProperty("version").GetInt64().Should().Be(Version);
        body.GetProperty("updatedAt").GetDateTimeOffset().Should().Be(LastEventAt);
        body.GetProperty("isPublished").GetBoolean().Should().BeFalse();
        body.GetProperty("isDisabledByAdmin").GetBoolean().Should().BeTrue();
        body.GetProperty("disabledAt").GetDateTimeOffset().Should().Be(DisabledAt);
        body.GetProperty("disabledBy").GetString().Should().Be(Orchestrator.AdminUsername);
        ShouldBeSameJson(body.GetProperty("payload"), Payload);
    }

    [Test]
    public async Task DefaultUser_WithUnpublishedContentEntity()
    {
        var reader = await Orchestrator.CreateUserAsync(Orchestrator.ReaderUsername, UserRole.User);
        await Orchestrator.SeedEntityAsync(Seeded(isPublished: false));

        var response = await Orchestrator.GetContentEntityAsync(ContentEntityId, reader);

        await ShouldBeNotFoundAsync(response);
    }

    [Test]
    public async Task DefaultUser_WithDisabledContentEntity()
    {
        var reader = await Orchestrator.CreateUserAsync(Orchestrator.ReaderUsername, UserRole.User);
        await Orchestrator.SeedEntityAsync(
            Orchestrator.Disable(Seeded(isPublished: true), Orchestrator.AdminUsername, DisabledAt)
        );

        var response = await Orchestrator.GetContentEntityAsync(ContentEntityId, reader);

        await ShouldBeNotFoundAsync(response);
    }

    [Test]
    public async Task AdminUser_WithUnknownContentEntity()
    {
        var admin = await Orchestrator.CreateUserAsync(Orchestrator.AdminUsername, UserRole.Admin);

        var response = await Orchestrator.GetContentEntityAsync(ContentEntityId, admin);

        await ShouldBeNotFoundAsync(response);
    }

    private const string ContentEntityId = "article-1";
    private const long Version = 3;

    // Nested on purpose: the payload is written through as JSON, not as a string.
    private const string Payload = """{"title": "Hello", "tags": ["a", "b"], "meta": {"n": 1}}""";

    private static readonly DateTimeOffset LastEventAt = new(2026, 9, 29, 10, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset DisabledAt = new(2026, 9, 29, 11, 0, 0, TimeSpan.Zero);

    private static ContentEntity Seeded(bool isPublished) =>
        Orchestrator.NewContentEntity(ContentEntityId, LastEventAt, isPublished, Version, Payload);

    private static void ShouldBeSameJson(JsonElement actual, string expected)
    {
        actual.ValueKind.Should().Be(JsonValueKind.Object);
        JsonNode
            .DeepEquals(JsonNode.Parse(actual.GetRawText()), JsonNode.Parse(expected))
            .Should()
            .BeTrue();
    }

    private static Task ShouldBeNotFoundAsync(HttpResponseMessage response) =>
        response.ShouldBeProblemAsync(HttpStatusCode.NotFound);
}
