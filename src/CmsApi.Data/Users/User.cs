using CmsApi.Core.Domain.Users;

namespace CmsApi.Data.Users;

/// <summary>A person who logs in with Basic auth. <see cref="Username"/> is stored lowercase.</summary>
public sealed class User
{
    public Guid Id { get; set; }

    public required string Username { get; set; }

    /// <summary>Argon2id PHC string.</summary>
    public required string PasswordHash { get; set; }

    public UserRole Role { get; set; }
}
