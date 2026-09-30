using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using CmsApi.Core.Domain.Auth;
using CmsApi.Core.Domain.Users;
using CmsApi.Data.ContentEntities;
using FluentAssertions;
using Isopoh.Cryptography.Argon2;
using Microsoft.Net.Http.Headers;

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

    [TestCase(null)]
    [TestCase("Basic not-base64!")]
    public async Task AnonymousUser_WithMissingOrMalformedHeader(string? authorization)
    {
        await Orchestrator.SeedEntityAsync(Seeded(isPublished: true));

        var response = await GetWithAuthorizationAsync(authorization);

        await response.ShouldBeChallengedAsync();
    }

    [Test]
    public async Task AnonymousUser_WithUnknownUsername()
    {
        await Orchestrator.SeedEntityAsync(Seeded(isPublished: true));

        var response = await Orchestrator.GetContentEntityAsync(
            ContentEntityId,
            new BasicCredentials("nobody", "password")
        );

        await response.ShouldBeChallengedAsync();
    }

    [Test]
    public async Task DefaultUser_WithWrongPassword()
    {
        var reader = await Orchestrator.CreateUserAsync(Orchestrator.ReaderUsername, UserRole.User);
        await Orchestrator.SeedEntityAsync(Seeded(isPublished: true));

        var response = await Orchestrator.GetContentEntityAsync(
            ContentEntityId,
            reader with
            {
                Password = "wrong-password",
            }
        );

        await response.ShouldBeChallengedAsync();
    }

    [Test]
    public async Task DefaultUser_WithUppercaseUsername()
    {
        var reader = await Orchestrator.CreateUserAsync(Orchestrator.ReaderUsername, UserRole.User);
        await Orchestrator.SeedEntityAsync(Seeded(isPublished: true));

        var response = await Orchestrator.GetContentEntityAsync(
            ContentEntityId,
            reader with
            {
                Username = Orchestrator.ReaderUsername.ToUpperInvariant(),
            }
        );

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Test]
    public async Task DefaultUser_CreatedWithMixedCaseUsername()
    {
        var reader = await Orchestrator.CreateUserAsync("Reader", UserRole.User);
        await Orchestrator.SeedEntityAsync(Seeded(isPublished: true));

        var response = await Orchestrator.GetContentEntityAsync(
            ContentEntityId,
            reader with
            {
                Username = Orchestrator.ReaderUsername,
            }
        );

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await Orchestrator.ReadUsersAsync())
            .Should()
            .ContainSingle()
            .Which.Username.Should()
            .Be(Orchestrator.ReaderUsername);
    }

    [Test]
    public async Task DefaultUser_WithOutdatedPasswordHash()
    {
        var outdatedHash = Argon2.Hash(OutdatedPassword, timeCost: 1, memoryCost: 8 * 1024);
        await Orchestrator.CreateUserAsync(
            new StoredUser(Guid.NewGuid(), Orchestrator.ReaderUsername, outdatedHash, UserRole.User)
        );
        await Orchestrator.SeedEntityAsync(Seeded(isPublished: true));

        var response = await Orchestrator.GetContentEntityAsync(
            ContentEntityId,
            new BasicCredentials(Orchestrator.ReaderUsername, OutdatedPassword)
        );

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var stored = (await Orchestrator.ReadUsersAsync()).Should().ContainSingle().Subject;
        stored.PasswordHash.Should().StartWith(CurrentHashSettings);
        Argon2.Verify(stored.PasswordHash, OutdatedPassword).Should().BeTrue();
    }

    [Test]
    public async Task DefaultUser_WithCachedCredentials()
    {
        var reader = await Orchestrator.CreateUserAsync(Orchestrator.ReaderUsername, UserRole.User);
        await Orchestrator.SeedEntityAsync(Seeded(isPublished: true));
        await Orchestrator.GetContentEntityAsync(ContentEntityId, reader);
        await Orchestrator.DeleteUsersAsync();

        var response = await Orchestrator.GetContentEntityAsync(ContentEntityId, reader);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Test]
    public async Task CmsClient_WithVisibleContentEntity()
    {
        await Orchestrator.SeedEntityAsync(Seeded(isPublished: true));

        var response = await Orchestrator.GetContentEntityAsync(
            ContentEntityId,
            Orchestrator.CmsClientCredentials()
        );

        await response.ShouldBeProblemAsync(HttpStatusCode.Forbidden);
    }

    private const string ContentEntityId = "article-1";
    private const string OutdatedPassword = "outdated-password";

    private const string CurrentHashSettings = "$argon2id$v=19$m=19456,t=2,p=1$";
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

    private static async Task<HttpResponseMessage> GetWithAuthorizationAsync(string? authorization)
    {
        using var client = Orchestrator.CreateClient();
        if (authorization is not null)
        {
            client.DefaultRequestHeaders.TryAddWithoutValidation(
                HeaderNames.Authorization,
                authorization
            );
        }

        return await client.GetAsync($"{Orchestrator.ContentEntitiesRoute}/{ContentEntityId}");
    }

    private static Task ShouldBeNotFoundAsync(HttpResponseMessage response) =>
        response.ShouldBeProblemAsync(HttpStatusCode.NotFound);
}
