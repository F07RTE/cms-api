using System.Security.Claims;
using CmsApi.Core.Domain.Users;

namespace CmsApi.Auth.AuthorizeCaller;

public static class ClaimsPrincipalExtensions
{
    private const string MissingNameClaim = "The authenticated caller has no name claim.";

    public static UserRole UserRole(this ClaimsPrincipal principal) =>
        principal.IsInRole(AuthNames.AdminRole)
            ? Core.Domain.Users.UserRole.Admin
            : Core.Domain.Users.UserRole.User;

    public static string Username(this ClaimsPrincipal principal) =>
        principal.FindFirstValue(ClaimTypes.Name)
        ?? throw new InvalidOperationException(MissingNameClaim);
}
