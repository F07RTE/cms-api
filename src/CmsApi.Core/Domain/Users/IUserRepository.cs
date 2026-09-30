namespace CmsApi.Core.Domain.Users;

public interface IUserRepository
{
    Task<StoredUser?> FindByUsernameAsync(string username, CancellationToken cancellationToken);

    Task UpdatePasswordHashAsync(
        Guid userId,
        string passwordHash,
        CancellationToken cancellationToken
    );
}
