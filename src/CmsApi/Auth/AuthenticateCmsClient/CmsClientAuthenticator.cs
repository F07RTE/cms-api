using System.Security.Cryptography;
using System.Text;
using CmsApi.Core.Domain.Auth;
using Microsoft.Extensions.Options;

namespace CmsApi.Auth.AuthenticateCmsClient;

/// <summary>Checks Basic credentials against the one CMS Client credential from config.</summary>
public sealed class CmsClientAuthenticator(IOptions<CmsCredentials> cmsCredentials)
{
    // Both fields are always compared, so the time taken doesn't reveal which one was wrong.
    public bool IsCmsClient(BasicCredentials credentials)
    {
        var expected = cmsCredentials.Value;
        var usernameMatches = FixedTimeEquals(credentials.Username, expected.Username);
        var passwordMatches = FixedTimeEquals(credentials.Password, expected.Password);
        return usernameMatches & passwordMatches;
    }

    private static bool FixedTimeEquals(string actual, string expected) =>
        CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(actual),
            Encoding.UTF8.GetBytes(expected)
        );
}
