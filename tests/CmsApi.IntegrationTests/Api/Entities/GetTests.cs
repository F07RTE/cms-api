using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CmsApi.Core.Domain.Auth;
using CmsApi.Core.Domain.Users;
using CmsApi.Data.ContentEntities;
using FluentAssertions;

namespace CmsApi.IntegrationTests.Api.Entities;

public sealed class GetTests : IntegrationTest
{
    [Test]
    public async Task DefaultUser_WithMixedContentEntities()
    {
        var reader = await Orchestrator.CreateUserAsync(Orchestrator.ReaderUsername, UserRole.User);
        await SeedMixedAsync();

        var response = await Orchestrator.ListContentEntitiesAsync(reader);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Ids(body).Should().Equal("newer", "older");
        body.GetProperty("nextCursor").ValueKind.Should().Be(JsonValueKind.Null);
        var newest = body.GetProperty("items")[0];
        newest.PropertyNames().Should().BeEquivalentTo("id", "version", "payload", "updatedAt");
        newest.GetProperty("updatedAt").GetDateTimeOffset().Should().Be(Newest);
        newest.GetProperty("payload").GetProperty("title").GetString().Should().Be("Hello");
    }

    [Test]
    public async Task AdminUser_WithMixedContentEntities()
    {
        var admin = await Orchestrator.CreateUserAsync(Orchestrator.AdminUsername, UserRole.Admin);
        await SeedMixedAsync();

        var response = await Orchestrator.ListContentEntitiesAsync(admin);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Ids(body).Should().Equal("disabled", "unpublished", "newer", "older");
        body.GetProperty("nextCursor").ValueKind.Should().Be(JsonValueKind.Null);
        var disabled = body.GetProperty("items")[0];
        disabled.GetProperty("isPublished").GetBoolean().Should().BeTrue();
        disabled.GetProperty("isDisabledByAdmin").GetBoolean().Should().BeTrue();
        disabled.GetProperty("disabledAt").GetDateTimeOffset().Should().Be(DisabledAt);
        disabled.GetProperty("disabledBy").GetString().Should().Be(Orchestrator.AdminUsername);
    }

    [Test]
    public async Task DefaultUser_WithTwoPages()
    {
        var reader = await Orchestrator.CreateUserAsync(Orchestrator.ReaderUsername, UserRole.User);
        // "b" and "c" tie on updatedAt and straddle the page boundary: the id breaks the tie.
        await Orchestrator.SeedEntityAsync(Seeded("c", Oldest, isPublished: true));
        await Orchestrator.SeedEntityAsync(Seeded("a", Newest, isPublished: true));
        await Orchestrator.SeedEntityAsync(Seeded("b", Oldest, isPublished: true));

        var first = await ReadPageAsync(reader, "?limit=2");
        var nextCursor = first.GetProperty("nextCursor").GetString();
        var second = await ReadPageAsync(reader, $"?limit=2&cursor={nextCursor}");

        Ids(first).Should().Equal("a", "b");
        nextCursor.Should().NotBeNullOrEmpty();
        Ids(second).Should().Equal("c");
        second.GetProperty("nextCursor").ValueKind.Should().Be(JsonValueKind.Null);
    }

    [TestCase("")]
    [TestCase("0")]
    [TestCase("201")]
    [TestCase("abc")]
    public async Task DefaultUser_WithInvalidLimit(string limit)
    {
        var reader = await Orchestrator.CreateUserAsync(Orchestrator.ReaderUsername, UserRole.User);

        var response = await Orchestrator.ListContentEntitiesAsync(reader, $"?limit={limit}");

        await response.ShouldBeProblemAsync(HttpStatusCode.BadRequest);
    }

    [TestCase("")]
    [TestCase("not-a-cursor")]
    public async Task DefaultUser_WithMalformedCursor(string cursor)
    {
        var reader = await Orchestrator.CreateUserAsync(Orchestrator.ReaderUsername, UserRole.User);

        var response = await Orchestrator.ListContentEntitiesAsync(reader, $"?cursor={cursor}");

        await response.ShouldBeProblemAsync(HttpStatusCode.BadRequest);
    }

    private const string Payload = """{"title": "Hello"}""";

    private static readonly DateTimeOffset Oldest = new(2026, 9, 29, 8, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Newest = new(2026, 9, 29, 11, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset DisabledAt = new(2026, 9, 29, 15, 0, 0, TimeSpan.Zero);

    private static async Task SeedMixedAsync()
    {
        await Orchestrator.SeedEntityAsync(Seeded("older", Oldest, isPublished: true));
        await Orchestrator.SeedEntityAsync(Seeded("newer", Newest, isPublished: true));
        await Orchestrator.SeedEntityAsync(
            Seeded("unpublished", Newest.AddHours(1), isPublished: false)
        );
        await Orchestrator.SeedEntityAsync(
            Orchestrator.Disable(
                Seeded("disabled", Newest.AddHours(2), isPublished: true),
                Orchestrator.AdminUsername,
                DisabledAt
            )
        );
    }

    private static ContentEntity Seeded(string id, DateTimeOffset lastEventAt, bool isPublished) =>
        Orchestrator.NewContentEntity(id, lastEventAt, isPublished, payload: Payload);

    private static async Task<JsonElement> ReadPageAsync(BasicCredentials reader, string query)
    {
        var response = await Orchestrator.ListContentEntitiesAsync(reader, query);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }

    private static IEnumerable<string?> Ids(JsonElement body) =>
        body.GetProperty("items")
            .EnumerateArray()
            .Select(item => item.GetProperty("id").GetString());
}
