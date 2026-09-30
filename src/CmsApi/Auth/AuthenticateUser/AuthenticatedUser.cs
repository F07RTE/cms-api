using CmsApi.Core.Domain.Users;

namespace CmsApi.Auth.AuthenticateUser;

/// <summary>A User whose password checked out. Holds no secret, so it can be cached.</summary>
public sealed record AuthenticatedUser(Guid Id, string Username, UserRole Role);
