using CmsApi.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Respawn;
using Respawn.Graph;

namespace CmsApi.IntegrationTests;

public static class Orchestrator
{
    private const string TestingEnvironment = "Testing";

    private static readonly WebApplicationFactory<Program> Factory =
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            builder.UseEnvironment(TestingEnvironment)
        );

    private static Respawner? respawner;

    public static AsyncServiceScope CreateScope() => Factory.Services.CreateAsyncScope();

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
