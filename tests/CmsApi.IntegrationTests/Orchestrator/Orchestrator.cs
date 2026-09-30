using CmsApi.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;
using Respawn;
using Respawn.Graph;

// One class split by purpose across this folder. The namespace stays at the root: a
// ...IntegrationTests.Orchestrator namespace would clash with the class name.
namespace CmsApi.IntegrationTests;

/// <summary>The test host and every helper a test drives it with. This file: the host and database.</summary>
public static partial class Orchestrator
{
    private const string TestingEnvironment = "Testing";

    /// <summary>The clock every host service reads. Starts on a whole second, so Postgres stores it exactly.</summary>
    public static readonly FakeTimeProvider Clock = new(
        new DateTimeOffset(2026, 9, 29, 12, 0, 0, TimeSpan.Zero)
    );

    private static readonly RecordedLogs Logs = new();

    private static readonly WebApplicationFactory<Program> Factory =
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            builder
                .UseEnvironment(TestingEnvironment)
                .ConfigureLogging(logging => logging.AddProvider(Logs))
                .ConfigureTestServices(services => services.AddSingleton<TimeProvider>(Clock))
        );

    private static Respawner? respawner;

    public static AsyncServiceScope CreateScope() => Factory.Services.CreateAsyncScope();

    /// <summary>The logs the host services wrote for <paramref name="batchId"/>.</summary>
    public static List<RecordedLog> ReadLogsForBatch(long batchId) => Logs.ForBatch(batchId);

    /// <summary>Forgets the logs of earlier tests.</summary>
    public static void ClearLogs() => Logs.Clear();

    public static Task MigrateDatabaseAsync() =>
        WithWriterAsync(context => context.Database.MigrateAsync());

    public static async Task ResetDatabaseAsync()
    {
        await using var scope = CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<WriteDbContext>();
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

    // Each helper gets a fresh scope, so its context never sees another's tracked rows.
    private static async Task<T> WithWriterAsync<T>(Func<WriteDbContext, Task<T>> action)
    {
        await using var scope = CreateScope();
        return await action(scope.ServiceProvider.GetRequiredService<WriteDbContext>());
    }

    private static async Task WithWriterAsync(Func<WriteDbContext, Task> action)
    {
        await using var scope = CreateScope();
        await action(scope.ServiceProvider.GetRequiredService<WriteDbContext>());
    }
}
