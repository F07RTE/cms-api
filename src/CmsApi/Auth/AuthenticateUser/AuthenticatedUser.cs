using CmsApi.Core.Domain.Users;

namespace CmsApi.Auth.AuthenticateUser;

// Holds no secret, so it can be cached.
public sealed record AuthenticatedUser(Guid Id, string Username, UserRole Role);
