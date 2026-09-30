namespace CmsApi.Auth.AuthenticateCmsClient;

/// <summary>The one CMS Client credential. Plaintext in the secret store; compared in fixed time.</summary>
public sealed class CmsCredentials
{
    public const string SectionName = "CmsCredentials";
    public const int MinUsernameLength = 10;
    public const int MaxUsernameLength = 20;

    public string Username { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;
}
