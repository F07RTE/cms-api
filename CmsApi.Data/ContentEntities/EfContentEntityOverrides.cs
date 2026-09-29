using CmsApi.Core.ContentEntities;

namespace CmsApi.Data.ContentEntities;

/// <summary>
/// Writes the admin columns only, under the row lock, and maps the locked row with
/// <paramref name="project"/>, so the answer comes from the writer.
/// </summary>
internal sealed class EfContentEntityOverrides(
    WriteDbContext context,
    TimeProvider timeProvider,
    Func<ContentEntity, IProjectedContentEntity> project
) : IContentEntityOverrides
{
    public Task<IProjectedContentEntity?> DisableAsync(
        string id,
        string adminUsername,
        CancellationToken cancellationToken
    ) =>
        OverrideAsync(
            id,
            row => row.Disable(adminUsername, timeProvider.GetUtcNow()),
            cancellationToken
        );

    public Task<IProjectedContentEntity?> EnableAsync(
        string id,
        CancellationToken cancellationToken
    ) => OverrideAsync(id, row => row.Enable(), cancellationToken);

    // Already in the target state: change returns false and nothing is written.
    private async Task<IProjectedContentEntity?> OverrideAsync(
        string id,
        Func<ContentEntity, bool> change,
        CancellationToken cancellationToken
    )
    {
        await using var transaction = await context.Database.BeginTransactionAsync(
            cancellationToken
        );
        var row = await context.LockContentEntityAsync(id, cancellationToken);
        if (row is not null && change(row))
        {
            await context.SaveChangesAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
        return row is null ? null : project(row);
    }
}
