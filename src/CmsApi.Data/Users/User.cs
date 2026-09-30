using CmsApi.Core.Domain.Users;

namespace CmsApi.Data.Users;

public sealed class User
{
    public Guid Id { get; set; }

    public required string Username { get; set; }

    public required string PasswordHash { get; set; }

    public UserRole Role { get; set; }
}
