using CmsApi.Core.Users;
using CmsApi.Data;

namespace CmsApi.Dtos;

public static class ContentEntityReaders
{
    /// <summary>One Content Entity reader per role, each projecting to that role's DTO.</summary>
    public static IServiceCollection AddContentEntityReaders(this IServiceCollection services) =>
        services
            .AddContentEntityReader(UserRole.User, ContentEntityResponse.Projection)
            .AddContentEntityReader(UserRole.Admin, AdminContentEntityResponse.Projection);
}
