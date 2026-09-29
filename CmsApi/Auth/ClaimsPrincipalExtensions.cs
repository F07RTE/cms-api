using System.Security.Claims;
using CmsApi.Core.Users;

namespace CmsApi.Auth;

public static class ClaimsPrincipalExtensions
{
    /// <summary>The role of a User or Admin caller, read back from the role claim.</summary>
    public static UserRole UserRole(this ClaimsPrincipal principal) =>
        principal.IsInRole(AuthNames.AdminRole)
            ? Core.Users.UserRole.Admin
            : Core.Users.UserRole.User;
}
