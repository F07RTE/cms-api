namespace CmsApi.Core.Domain.Users;

public interface IUserRepository
{
    /// <summary>Finds a User by username, case-insensitively. Null when there is none.</summary>
    Task<StoredUser?> FindByUsernameAsync(string username, CancellationToken cancellationToken);

    Task UpdatePasswordHashAsync(
        Guid userId,
        string passwordHash,
        CancellationToken cancellationToken
    );
}
