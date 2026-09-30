using Microsoft.EntityFrameworkCore;

namespace CmsApi.Data.ContentEntities;

internal static class ContentEntityLocks
{
    /// <summary>
    /// Loads and row-locks the Content Entity until the open transaction ends; null when absent.
    /// Serialises the worker against an Admin PATCH on the same row.
    /// </summary>
    public static async Task<ContentEntity?> LockContentEntityAsync(
        this WriteDbContext context,
        string id,
        CancellationToken cancellationToken
    )
    {
        var rows = await context
            .ContentEntities.FromSql($"SELECT * FROM content_entities WHERE id = {id} FOR UPDATE")
            .ToListAsync(cancellationToken);
        return rows.SingleOrDefault();
    }
}
