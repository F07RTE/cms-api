using CmsApi.Core.Domain.Users;

namespace CmsApi.Auth.AuthenticateUser;

public sealed record AuthenticatedUser(Guid Id, string Username, UserRole Role);
