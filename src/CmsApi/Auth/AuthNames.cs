using CmsApi.Core.Domain.Auth;
using CmsApi.Core.Domain.Users;

namespace CmsApi.Auth;

public static class AuthNames
{
    public const string BasicScheme = BasicCredentials.Scheme;

    public const string CmsClientRole = "Cms";

    public const string DefaultUserRole = nameof(UserRole.User);

    public const string AdminRole = nameof(UserRole.Admin);

    public const string CmsClientPolicy = "Cms";

    public const string ApiUserPolicy = "ApiUser";

    public const string AdminOnlyPolicy = "AdminOnly";
}
