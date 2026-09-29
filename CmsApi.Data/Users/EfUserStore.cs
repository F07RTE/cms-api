using CmsApi.Core.Users;
using Microsoft.EntityFrameworkCore;

namespace CmsApi.Data.Users;

internal sealed class EfUserStore(ReadDbContext reader, IDbContextFactory<WriteDbContext> writers)
    : IUserStore
{
    public async Task<StoredUser?> FindByUsernameAsync(
        string username,
        CancellationToken cancellationToken
    )
    {
        var lowercase = username.ToLowerInvariant();
        var user = await reader.Users.SingleOrDefaultAsync(
            candidate => candidate.Username == lowercase,
            cancellationToken
        );
        return user is null
            ? null
            : new StoredUser(user.Id, user.Username, user.PasswordHash, user.Role);
    }

    // Through the writer, which the reader may be a replica of. Opened only here, so a login that
    // needs no rehash never touches the writer.
    public async Task UpdatePasswordHashAsync(
        Guid userId,
        string passwordHash,
        CancellationToken cancellationToken
    )
    {
        await using var writer = await writers.CreateDbContextAsync(cancellationToken);
        await writer
            .Users.Where(user => user.Id == userId)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(user => user.PasswordHash, passwordHash),
                cancellationToken
            );
    }
}
