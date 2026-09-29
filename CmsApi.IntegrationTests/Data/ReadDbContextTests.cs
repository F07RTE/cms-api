using CmsApi.Data;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace CmsApi.IntegrationTests.Data;

public sealed class ReadDbContextTests : IntegrationTest
{
    [Test]
    public async Task Caller_WithInsertStatement()
    {
        await using var scope = Orchestrator.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ReadDbContext>();

        var insert = () => context.Database.ExecuteSqlRawAsync(InsertIntoMigrationsHistory);

        (await insert.Should().ThrowAsync<PostgresException>())
            .Which.SqlState.Should()
            .Be(PostgresErrorCodes.InsufficientPrivilege);
    }

    // The migrations history is the one table the skeleton has; the reader can only SELECT from it.
    private const string ProbeValue = "probe";

    private static readonly string InsertIntoMigrationsHistory =
        $"""INSERT INTO "{HistoryRepository.DefaultTableName}" VALUES ('{ProbeValue}', '{ProbeValue}')""";
}
