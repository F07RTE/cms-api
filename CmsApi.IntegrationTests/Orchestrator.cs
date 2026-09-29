using System.Net.Http.Headers;
using System.Net.Mime;
using System.Text;
using System.Text.Json;
using CmsApi.Auth;
using CmsApi.Core.Auth;
using CmsApi.Data;
using CmsApi.Data.Inbox;
using Microsoft.AspNetCore.Hosting;
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
}
