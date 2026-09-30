using CmsApi.Core.Domain.Inbox;
using Microsoft.Extensions.Options;
using Npgsql;

namespace CmsApi.Data.Inbox;

// A session-level advisory lock on a connection of its own: losing the connection loses the lock.
internal sealed class PgLeaderLock(IOptions<ConnectionStringOptions> options) : ILeaderLock
{
    // Any fixed key works; every worker has to use the same one.
    private const long LockKey = 0x434D535F_496E626F;
    private const string KeyParameter = "key";

    private const string TryLockSql = $"SELECT pg_try_advisory_lock(@{KeyParameter})";
    private const string UnlockSql = $"SELECT pg_advisory_unlock(@{KeyParameter})";

    // A session lock is held until unlocked or the session ends: a live session still holds it.
    private const string IsSessionAliveSql = "SELECT true";

    private NpgsqlConnection? connection;

    public async Task<bool> TryAcquireAsync(CancellationToken cancellationToken)
    {
        if (connection is not null)
        {
            return await IsHeldAsync(cancellationToken);
        }

        connection = new NpgsqlConnection(UnpooledConnectionString());
        var acquired = false;
        try
        {
            await connection.OpenAsync(cancellationToken);
            acquired = await QueryFlagAsync(TryLockSql, cancellationToken);
        }
        finally
        {
            if (!acquired)
            {
                await CloseAsync();
            }
        }

        return acquired;
    }

    public async Task<bool> IsHeldAsync(CancellationToken cancellationToken)
    {
        if (connection is null)
        {
            return false;
        }

        try
        {
            return await QueryFlagAsync(IsSessionAliveSql, cancellationToken);
        }
        catch (NpgsqlException)
        {
            // The session is gone, and the lock with it.
            await CloseAsync();
            return false;
        }
    }

    public ValueTask DisposeAsync() => new(StepDownAsync());

    // Unlocks explicitly so the next leader need not wait for the server to end this session.
    public async Task StepDownAsync()
    {
        if (connection is null)
        {
            return;
        }

        try
        {
            await QueryFlagAsync(UnlockSql, CancellationToken.None);
        }
        catch (Exception exception) when (exception is NpgsqlException or InvalidOperationException)
        {
            // The connection is broken; closing the session below releases the lock anyway.
        }

        await CloseAsync();
    }

    private async Task CloseAsync()
    {
        if (connection is not null)
        {
            await connection.DisposeAsync();
            connection = null;
        }
    }

    private async Task<bool> QueryFlagAsync(string sql, CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue(KeyParameter, LockKey);
        return await command.ExecuteScalarAsync(cancellationToken) is true;
    }

    // A pooled connection outlives Dispose, and so would the lock it holds.
    private string UnpooledConnectionString() =>
        new NpgsqlConnectionStringBuilder(options.Value.Writer)
        {
            Pooling = false,
        }.ConnectionString;
}
