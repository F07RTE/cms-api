namespace CmsApi.Auth;

public static class AuthPolicies
{
    /// <summary>Only the CMS Client may deliver Batches.</summary>
    public const string CmsClient = "Cms";
}
