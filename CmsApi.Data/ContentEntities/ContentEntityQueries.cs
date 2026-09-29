using System.Linq.Expressions;
using CmsApi.Core.ContentEntities;
using CmsApi.Core.Users;
using Microsoft.EntityFrameworkCore;

namespace CmsApi.Data.ContentEntities;

/// <summary>
/// The compiled reads of one role's shape: filtered to what the role may see and projected
/// straight to <typeparamref name="T"/>. Compiled once, so keep one instance per shape.
/// </summary>
internal sealed class ContentEntityQueries<T>(
    UserRole role,
    Expression<Func<ContentEntity, T>> projection
)
    where T : class, IProjectedContentEntity
{
    /// <summary>The Content Entity with this id, or null when the role can't see it.</summary>
    public Func<ReadDbContext, string, CancellationToken, Task<T?>> Find { get; } =
        CompileFind(ContentEntityVisibility.VisibleTo<ContentEntity>(role), projection);

    /// <summary>Up to <c>take</c> rows after <c>(updatedAt, id)</c>, in list order.</summary>
    public Func<
        ReadDbContext,
        DateTimeOffset,
        string,
        int,
        IAsyncEnumerable<T>
    > PageAfter { get; } =
        CompilePageAfter(ContentEntityVisibility.VisibleTo<ContentEntity>(role), projection);

    private static Func<ReadDbContext, string, CancellationToken, Task<T?>> CompileFind(
        Expression<Func<ContentEntity, bool>> visible,
        Expression<Func<ContentEntity, T>> projection
    ) =>
        EF.CompileAsyncQuery(
            (ReadDbContext reader, string id, CancellationToken cancellationToken) =>
                reader
                    .ContentEntities.Where(visible)
                    .Where(contentEntity => contentEntity.Id == id)
                    .Select(projection)
                    .SingleOrDefault()
        );

    // updatedAt desc then id, as the list indexes are. The <= alone is the index condition; the
    // rest drops the rows up to and including the cursor's.
    private static Func<
        ReadDbContext,
        DateTimeOffset,
        string,
        int,
        IAsyncEnumerable<T>
    > CompilePageAfter(
        Expression<Func<ContentEntity, bool>> visible,
        Expression<Func<ContentEntity, T>> projection
    ) =>
        EF.CompileAsyncQuery(
            (ReadDbContext reader, DateTimeOffset updatedAt, string id, int take) =>
                reader
                    .ContentEntities.Where(visible)
                    .Where(contentEntity =>
                        contentEntity.LastEventAt <= updatedAt
                        && (
                            contentEntity.LastEventAt < updatedAt
                            || string.Compare(contentEntity.Id, id) > 0
                        )
                    )
                    .OrderByDescending(contentEntity => contentEntity.LastEventAt)
                    .ThenBy(contentEntity => contentEntity.Id)
                    .Take(take)
                    .Select(projection)
        );
}
