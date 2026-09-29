using CmsApi.Core.Auth;
using CmsApi.Core.Users;

namespace CmsApi.Auth;

/// <summary>The names auth registers and checks: scheme, roles and policies.</summary>
public static class AuthNames
{
    public const string BasicScheme = BasicCredentials.Scheme;

    public const string CmsClientRole = "Cms";

    public const string DefaultUserRole = nameof(UserRole.User);

    public const string AdminRole = nameof(UserRole.Admin);

    /// <summary>Only the CMS Client may deliver Batches.</summary>
    public const string CmsClientPolicy = "Cms";

    /// <summary>Users and Admins read Content Entities.</summary>
    public const string ApiUserPolicy = "ApiUser";

    /// <summary>Only Admins disable and enable Content Entities.</summary>
    public const string AdminOnlyPolicy = "AdminOnly";
}
