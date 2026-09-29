using System.Net.Http.Headers;
using System.Net.Mime;
using System.Text;
using System.Text.Json;
using CmsApi.Auth;
using CmsApi.Core.Auth;
using CmsApi.Core.Batches;
using CmsApi.Core.Inbox;
using CmsApi.Core.Users;
using CmsApi.Data;
using CmsApi.Data.ContentEntities;
using CmsApi.Data.EventLog;
using CmsApi.Data.Inbox;
using CmsApi.Data.Tombstones;
using CmsApi.Data.Users;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using Respawn;
using Respawn.Graph;

namespace CmsApi.IntegrationTests;

public static class Orchestrator
{
    public const string BatchRoute = "/cms/events";
    public const string ContentEntitiesRoute = "/entities";
    public const string ReaderUsername = "reader";
    public const string AdminUsername = "admin";

    private const string TestingEnvironment = "Testing";

    /// <summary>The clock every host service reads. Starts on a whole second, so Postgres stores it exactly.</summary>
    public static readonly FakeTimeProvider Clock = new(
        new DateTimeOffset(2026, 9, 29, 12, 0, 0, TimeSpan.Zero)
    );

    private static readonly WebApplicationFactory<Program> Factory =
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            builder
                .UseEnvironment(TestingEnvironment)
                .ConfigureTestServices(services => services.AddSingleton<TimeProvider>(Clock))
        );

    private static Respawner? respawner;

    public static AsyncServiceScope CreateScope() => Factory.Services.CreateAsyncScope();

    public static HttpClient CreateClient() => Factory.CreateClient();

    public static HttpClient WithBasicAuth(HttpClient client, BasicCredentials credentials)
    {
        var token = Convert.ToBase64String(
            Encoding.UTF8.GetBytes($"{credentials.Username}:{credentials.Password}")
        );
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            BasicCredentials.Scheme,
            token
        );
        return client;
    }

    /// <summary>The CMS Client credential from <c>appsettings.Testing.json</c>.</summary>
    public static BasicCredentials CmsClientCredentials()
    {
        var credentials = Factory.Services.GetRequiredService<IOptions<CmsCredentials>>().Value;
        return new BasicCredentials(credentials.Username, credentials.Password);
    }

    /// <summary>Stores a User with a fresh random password; returns the credentials to log in with.</summary>
    public static async Task<BasicCredentials> CreateUserAsync(string username, UserRole role)
    {
        await using var scope = CreateScope();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<StoredUser>>();
        var password = Guid.NewGuid().ToString();
        var user = new StoredUser(Guid.NewGuid(), username, string.Empty, role);
        await CreateUserAsync(user with { PasswordHash = hasher.HashPassword(user, password) });
        return new BasicCredentials(username, password);
    }

    /// <summary>Stores a User with the given password hash, as-is.</summary>
    public static async Task CreateUserAsync(StoredUser user)
    {
        await using var scope = CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<WriteDbContext>();
        context.Users.Add(
            new User
            {
                Id = user.Id,
                Username = user.Username,
                PasswordHash = user.PasswordHash,
                Role = user.Role,
            }
        );
        await context.SaveChangesAsync();
    }

    /// <summary>Removes every User, e.g. to show a login was served from the credential cache.</summary>
    public static async Task DeleteUsersAsync()
    {
        await using var scope = CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<WriteDbContext>();
        await context.Users.ExecuteDeleteAsync();
    }

    public static async Task<List<User>> ReadUsersAsync()
    {
        await using var scope = CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<WriteDbContext>();
        return await context.Users.OrderBy(user => user.Username).ToListAsync();
    }

    public static async Task<HttpResponseMessage> GetContentEntityAsync(
        string id,
        BasicCredentials credentials
    )
    {
        using var client = WithBasicAuth(CreateClient(), credentials);
        return await client.GetAsync($"{ContentEntitiesRoute}/{id}");
    }

    /// <summary>Gets <c>/entities</c> with the query string as given, e.g. <c>?limit=2</c>.</summary>
    public static async Task<HttpResponseMessage> ListContentEntitiesAsync(
        BasicCredentials credentials,
        string query = ""
    )
    {
        using var client = WithBasicAuth(CreateClient(), credentials);
        return await client.GetAsync($"{ContentEntitiesRoute}{query}");
    }

    /// <summary>A <c>publish</c> or <c>unPublish</c> CMS Event, as the CMS Client sends it.</summary>
    public static object CmsEvent(
        string type,
        string id,
        long version,
        DateTimeOffset timestamp,
        object payload
    ) =>
        new
        {
            type,
            id,
            version,
            timestamp = timestamp.ToString("O"),
            payload,
        };

    /// <summary>A <c>delete</c> CMS Event, as the CMS Client sends it.</summary>
    public static object DeleteEvent(string id, DateTimeOffset timestamp) =>
        new
        {
            type = "delete",
            id,
            timestamp = timestamp.ToString("O"),
        };

    public static Task<HttpResponseMessage> PostBatchAsync(IEnumerable<object> events) =>
        PostBatchAsync(JsonSerializer.Serialize(events));

    /// <summary>Posts the body as given, as the CMS Client.</summary>
    public static Task<HttpResponseMessage> PostBatchAsync(string body) =>
        PostBatchAsync(Encoding.UTF8.GetBytes(body), CmsClientCredentials());

    /// <summary>Posts the raw bytes. <paramref name="credentials"/> null posts anonymously.</summary>
    public static async Task<HttpResponseMessage> PostBatchAsync(
        byte[] body,
        BasicCredentials? credentials
    )
    {
        using var client = credentials is null
            ? CreateClient()
            : WithBasicAuth(CreateClient(), credentials);
        using var content = new ByteArrayContent(body);
        content.Headers.ContentType = new MediaTypeHeaderValue(MediaTypeNames.Application.Json);
        return await client.PostAsync(BatchRoute, content);
    }

    public static async Task<List<InboxBatch>> ReadInboxAsync()
    {
        await using var scope = CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<WriteDbContext>();
        return await context.InboxBatches.ToListAsync();
    }

    /// <summary>Runs the worker's processing until the Inbox has nothing due. No waiting.</summary>
    public static Task DrainInboxAsync() =>
        DrainInboxAsync(provider => provider.GetRequiredService<InboxProcessor>());

    /// <summary>Drains the Inbox with <paramref name="batchProcessor"/> in place of the real one.</summary>
    public static Task DrainInboxAsync(IBatchProcessor batchProcessor) =>
        DrainInboxAsync(provider =>
            ActivatorUtilities.CreateInstance<InboxProcessor>(provider, batchProcessor)
        );

    /// <summary>Claims the next due Batch and never finishes it, as a crashed worker would.</summary>
    public static async Task OrphanNextBatchAsync()
    {
        await using var scope = CreateScope();
        var inbox = scope.ServiceProvider.GetRequiredService<IInbox>();
        await inbox.ClaimNextAsync(CancellationToken.None);
    }

    /// <summary>Runs the recovery a worker runs when it becomes leader.</summary>
    public static async Task RecoverOrphansAsync()
    {
        await using var scope = CreateScope();
        var inbox = scope.ServiceProvider.GetRequiredService<IInbox>();
        await inbox.RecoverOrphansAsync(CancellationToken.None);
    }

    /// <summary>A fresh leader lock, as one worker replica holds it.</summary>
    public static ILeaderLock CreateLeaderLock() =>
        Factory.Services.GetRequiredService<ILeaderLock>();

    /// <summary>Puts every Batch back to Pending, as a retry or crash recovery would.</summary>
    public static async Task RequeueBatchesAsync()
    {
        await using var scope = CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<WriteDbContext>();
        await context.InboxBatches.ExecuteUpdateAsync(setters =>
            setters.SetProperty(batch => batch.Status, InboxStatus.Pending)
        );
    }

    /// <summary>Stores a Visible Content Entity with placeholder CMS data.</summary>
    public static Task<ContentEntity> SeedVisibleContentEntityAsync(string id) =>
        SeedEntityAsync(NewContentEntity(id, Clock.GetUtcNow(), isPublished: true));

    /// <summary>A Content Entity as the worker would store it. Not saved: pass it to <see cref="SeedEntityAsync"/>.</summary>
    public static ContentEntity NewContentEntity(
        string id,
        DateTimeOffset lastEventAt,
        bool isPublished,
        long version = 1,
        string payload = "{}"
    ) =>
        new()
        {
            Id = id,
            Version = version,
            Payload = payload,
            IsPublished = isPublished,
            LastEventAt = lastEventAt,
        };

    /// <summary>Sets the admin columns as an Admin disabling it would.</summary>
    public static ContentEntity Disable(
        ContentEntity contentEntity,
        string adminUsername,
        DateTimeOffset disabledAt
    )
    {
        contentEntity.IsDisabledByAdmin = true;
        contentEntity.DisabledAt = disabledAt;
        contentEntity.DisabledBy = adminUsername;
        return contentEntity;
    }

    /// <summary>Stores a Content Entity directly, as if earlier Batches had created it.</summary>
    public static async Task<ContentEntity> SeedEntityAsync(ContentEntity contentEntity)
    {
        await using var scope = CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<WriteDbContext>();
        context.ContentEntities.Add(contentEntity);
        await context.SaveChangesAsync();
        return contentEntity;
    }

    public static async Task<List<ContentEntity>> ReadContentEntitiesAsync()
    {
        await using var scope = CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<WriteDbContext>();
        return await context.ContentEntities.OrderBy(entity => entity.Id).ToListAsync();
    }

    public static async Task<List<Tombstone>> ReadTombstonesAsync()
    {
        await using var scope = CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<WriteDbContext>();
        return await context.Tombstones.OrderBy(tombstone => tombstone.Id).ToListAsync();
    }

    public static async Task<List<EventLogEntry>> ReadEventLogAsync()
    {
        await using var scope = CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<WriteDbContext>();
        return await context.EventLog.OrderBy(entry => entry.Id).ToListAsync();
    }

    public static async Task MigrateDatabaseAsync()
    {
        await using var scope = CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<WriteDbContext>();
        await context.Database.MigrateAsync();
    }

    public static async Task ResetDatabaseAsync()
    {
        await using var scope = CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<WriteDbContext>();
        // Respawn refuses a database without tables; there is nothing to reset until the model has one.
        if (!context.Model.GetEntityTypes().Any())
        {
            return;
        }

        await context.Database.OpenConnectionAsync();
        var connection = context.Database.GetDbConnection();

        respawner ??= await Respawner.CreateAsync(
            connection,
            new RespawnerOptions
            {
                DbAdapter = DbAdapter.Postgres,
                TablesToIgnore = [new Table(HistoryRepository.DefaultTableName)],
            }
        );
        await respawner.ResetAsync(connection);
    }

    public static ValueTask DisposeAsync() => Factory.DisposeAsync();

    private static async Task DrainInboxAsync(
        Func<IServiceProvider, InboxProcessor> createProcessor
    )
    {
        bool processed;
        do
        {
            await using var scope = CreateScope();
            var processor = createProcessor(scope.ServiceProvider);
            processed = await processor.ProcessNextBatchAsync(CancellationToken.None);
        } while (processed);
    }
}
