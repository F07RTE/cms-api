using System.Net;
using System.Text;
using CmsApi.Core.Auth;
using CmsApi.Core.Users;
using FluentAssertions;
using Isopoh.Cryptography.Argon2;
using Microsoft.Net.Http.Headers;

namespace CmsApi.IntegrationTests.Auth;

public sealed class BasicAuthenticationHandlerTests : IntegrationTest
{
    [TestCase(null)]
    [TestCase("Bearer cmVhZGVyOnBhc3N3b3Jk")]
    [TestCase("Basic not-base64!")]
    [TestCase("Basic cmVhZGVy")] // "reader", no colon
    public async Task AnonymousUser_WithMissingOrMalformedHeader(string? authorization)
    {
        await Orchestrator.SeedVisibleContentEntityAsync(ContentEntityId);

        var response = await GetContentEntityAsync(authorization);

        await ShouldBeChallengedAsync(response);
    }

    [Test]
    public async Task AnonymousUser_WithUnknownUsername()
    {
        await Orchestrator.SeedVisibleContentEntityAsync(ContentEntityId);

        var response = await Orchestrator.GetContentEntityAsync(
            ContentEntityId,
            new BasicCredentials("nobody", "password")
        );

        await ShouldBeChallengedAsync(response);
    }

    [Test]
    public async Task DefaultUser_WithWrongPassword()
    {
        await Orchestrator.SeedVisibleContentEntityAsync(ContentEntityId);
        var reader = await Orchestrator.CreateUserAsync(ReaderUsername, UserRole.User);

        var response = await Orchestrator.GetContentEntityAsync(
            ContentEntityId,
            reader with
            {
                Password = "wrong-password",
            }
        );

        await ShouldBeChallengedAsync(response);
    }

    [Test]
    public async Task CmsClient_WithWrongPassword()
    {
        await Orchestrator.SeedVisibleContentEntityAsync(ContentEntityId);
        var cmsClient = Orchestrator.CmsClientCredentials();

        var response = await Orchestrator.GetContentEntityAsync(
            ContentEntityId,
            cmsClient with
            {
                Password = Guid.NewGuid().ToString(),
            }
        );

        await ShouldBeChallengedAsync(response);
    }

    [Test]
    public async Task CmsClient_WithValidCredentials()
    {
        var response = await Orchestrator.PostBatchAsync([
            Orchestrator.DeleteEvent(ContentEntityId, DateTimeOffset.UnixEpoch),
        ]);

        response.StatusCode.Should().Be(HttpStatusCode.Accepted);
    }

    [TestCase(UserRole.User)]
    [TestCase(UserRole.Admin)]
    public async Task User_WithValidCredentials(UserRole role)
    {
        await Orchestrator.SeedVisibleContentEntityAsync(ContentEntityId);
        var user = await Orchestrator.CreateUserAsync(ReaderUsername, role);

        var response = await Orchestrator.GetContentEntityAsync(ContentEntityId, user);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Test]
    public async Task DefaultUser_WithUppercaseUsername()
    {
        await Orchestrator.SeedVisibleContentEntityAsync(ContentEntityId);
        var reader = await Orchestrator.CreateUserAsync(ReaderUsername, UserRole.User);

        var response = await Orchestrator.GetContentEntityAsync(
            ContentEntityId,
            reader with
            {
                Username = ReaderUsername.ToUpperInvariant(),
            }
        );

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Test]
    public async Task DefaultUser_CreatedWithMixedCaseUsername()
    {
        await Orchestrator.SeedVisibleContentEntityAsync(ContentEntityId);
        var reader = await Orchestrator.CreateUserAsync("Reader", UserRole.User);

        var response = await Orchestrator.GetContentEntityAsync(
            ContentEntityId,
            reader with
            {
                Username = ReaderUsername,
            }
        );

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await Orchestrator.ReadUsersAsync())
            .Should()
            .ContainSingle()
            .Which.Username.Should()
            .Be(ReaderUsername);
    }

    [Test]
    public async Task DefaultUser_WithOutdatedPasswordHash()
    {
        await Orchestrator.SeedVisibleContentEntityAsync(ContentEntityId);
        var outdatedHash = Argon2.Hash(OutdatedPassword, timeCost: 1, memoryCost: 8 * 1024);
        await Orchestrator.CreateUserAsync(
            new StoredUser(Guid.NewGuid(), ReaderUsername, outdatedHash, UserRole.User)
        );

        var response = await Orchestrator.GetContentEntityAsync(
            ContentEntityId,
            new BasicCredentials(ReaderUsername, OutdatedPassword)
        );

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var stored = (await Orchestrator.ReadUsersAsync()).Should().ContainSingle().Subject;
        stored.PasswordHash.Should().StartWith(CurrentHashSettings);
        Argon2.Verify(stored.PasswordHash, OutdatedPassword).Should().BeTrue();
    }

    [Test]
    public async Task DefaultUser_WithCachedCredentials()
    {
        await Orchestrator.SeedVisibleContentEntityAsync(ContentEntityId);
        var reader = await Orchestrator.CreateUserAsync(ReaderUsername, UserRole.User);
        await Orchestrator.GetContentEntityAsync(ContentEntityId, reader);
        await Orchestrator.DeleteUsersAsync();

        var response = await Orchestrator.GetContentEntityAsync(ContentEntityId, reader);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Test]
    public async Task CmsClient_OnContentEntityRoute()
    {
        await Orchestrator.SeedVisibleContentEntityAsync(ContentEntityId);

        var response = await Orchestrator.GetContentEntityAsync(
            ContentEntityId,
            Orchestrator.CmsClientCredentials()
        );

        await response.ShouldBeProblemAsync(HttpStatusCode.Forbidden);
    }

    [TestCase(UserRole.User)]
    [TestCase(UserRole.Admin)]
    public async Task User_OnBatchRoute(UserRole role)
    {
        var user = await Orchestrator.CreateUserAsync(ReaderUsername, role);

        var response = await Orchestrator.PostBatchAsync(Encoding.UTF8.GetBytes("[]"), user);

        await response.ShouldBeProblemAsync(HttpStatusCode.Forbidden);
    }

    private const string ContentEntityId = "article-1";
    private const string ReaderUsername = "reader";
    private const string OutdatedPassword = "outdated-password";

    // Argon2id with the settings ADR 0003 fixes: m=19 MiB, t=2, p=1.
    private const string CurrentHashSettings = "$argon2id$v=19$m=19456,t=2,p=1$";

    // Sends the header as given: the malformed ones can't be built from BasicCredentials.
    private static async Task<HttpResponseMessage> GetContentEntityAsync(string? authorization)
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

    private static async Task ShouldBeChallengedAsync(HttpResponseMessage response)
    {
        await response.ShouldBeProblemAsync(HttpStatusCode.Unauthorized);
        response
            .Headers.WwwAuthenticate.Should()
            .ContainSingle()
            .Which.ToString()
            .Should()
            .Be("Basic realm=\"cms-api\", charset=\"UTF-8\"");
    }
}
