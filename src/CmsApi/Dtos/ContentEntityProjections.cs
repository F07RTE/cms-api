using CmsApi.Core.Domain.Users;
using CmsApi.Data;

namespace CmsApi.Dtos;

public static class ContentEntityProjections
{
    /// <summary>
    /// Wires the Content Entity read repositories to the DTOs they answer with: one per role, each
    /// projecting to that role's DTO.
    /// </summary>
    public static IServiceCollection AddContentEntityProjections(
        this IServiceCollection services
    ) =>
        services
            .AddContentEntityReadRepository(UserRole.User, ContentEntityResponse.Projection)
            .AddContentEntityReadRepository(UserRole.Admin, AdminContentEntityResponse.Projection);
}
