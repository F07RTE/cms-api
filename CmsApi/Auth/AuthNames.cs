using CmsApi.Core.Auth;

namespace CmsApi.Auth;

/// <summary>The names auth registers and checks: scheme, roles and policies.</summary>
public static class AuthNames
{
    public const string BasicScheme = BasicCredentials.Scheme;

    public const string CmsClientRole = "Cms";

    /// <summary>Only the CMS Client may deliver Batches.</summary>
    public const string CmsClientPolicy = "Cms";
}
