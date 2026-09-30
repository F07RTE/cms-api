using System.Linq.Expressions;
using CmsApi.Core.Domain.Users;

namespace CmsApi.Core.Domain.ContentEntities;

public static class ContentEntityVisibility
{
    /// <summary>
    /// A User sees Visible Content Entities only: published and not Disabled. An Admin sees every
    /// stored one. An expression, so a query can filter with it in SQL.
    /// </summary>
    public static Expression<Func<T, bool>> VisibleTo<T>(UserRole role)
        where T : IContentEntityFlags =>
        role == UserRole.Admin
            ? contentEntity => true
            : contentEntity => contentEntity.IsPublished && !contentEntity.IsDisabledByAdmin;
}
