using CmsApi.Core.Users;
using CmsApi.Data;

namespace CmsApi.Dtos;

public static class ContentEntityProjections
{
    /// <summary>
    /// Wires the Content Entity services to the DTOs they answer with: one reader per role, each
    /// projecting to that role's DTO, and the Admin overrides, answering with the Admin DTO.
    /// </summary>
    public static IServiceCollection AddContentEntityProjections(
        this IServiceCollection services
    ) =>
        services
            .AddContentEntityReader(UserRole.User, ContentEntityResponse.Projection)
            .AddContentEntityReader(UserRole.Admin, AdminContentEntityResponse.Projection)
            .AddContentEntityOverrides(AdminContentEntityResponse.Projection.Compile());
}
