using System.Security.Claims;
using CmsApi.Core.Users;

namespace CmsApi.Auth;

public static class ClaimsPrincipalExtensions
{
    private const string MissingNameClaim = "The authenticated caller has no name claim.";

    /// <summary>The role of a User or Admin caller, read back from the role claim.</summary>
    public static UserRole UserRole(this ClaimsPrincipal principal) =>
        principal.IsInRole(AuthNames.AdminRole)
            ? Core.Users.UserRole.Admin
            : Core.Users.UserRole.User;

    /// <summary>The caller's username, as authentication put it in the name claim.</summary>
    public static string Username(this ClaimsPrincipal principal) =>
        principal.FindFirstValue(ClaimTypes.Name)
        ?? throw new InvalidOperationException(MissingNameClaim);
}
