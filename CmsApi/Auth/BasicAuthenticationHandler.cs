using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using CmsApi.Core.Auth;
using CmsApi.Errors;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;

namespace CmsApi.Auth;

/// <summary>Basic auth. Checks the CMS Client credential; Users arrive in a later ticket.</summary>
public sealed class BasicAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    IOptions<CmsCredentials> cmsCredentials,
    IProblemDetailsService problemDetails
) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    private const string Challenge =
        BasicCredentials.Scheme + " realm=\"cms-api\", charset=\"UTF-8\"";
    private const string CmsClientId = "cms";
    private const string InvalidCredentials = "Invalid Basic credentials.";
    private const string ChallengeDetail = "Missing or invalid credentials.";
    private const string ChallengeAction =
        "Send valid credentials in a Basic Authorization header.";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.ContainsKey(HeaderNames.Authorization))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        if (
            !BasicCredentials.TryParse(Request.Headers.Authorization, out var credentials)
            || !IsCmsClient(credentials)
        )
        {
            return Task.FromResult(AuthenticateResult.Fail(InvalidCredentials));
        }

        return Task.FromResult(AuthenticateResult.Success(CmsClientTicket(credentials)));
    }

    protected override async Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        Response.StatusCode = StatusCodes.Status401Unauthorized;
        Response.Headers.WWWAuthenticate = Challenge;
        await problemDetails.WriteAsync(
            new ProblemDetailsContext
            {
                HttpContext = Context,
                ProblemDetails = Problems.Create(
                    StatusCodes.Status401Unauthorized,
                    ChallengeDetail,
                    ChallengeAction
                ),
            }
        );
    }

    // Both fields are always compared, so the time taken doesn't reveal which one was wrong.
    private bool IsCmsClient(BasicCredentials credentials)
    {
        var expected = cmsCredentials.Value;
        var usernameMatches = FixedTimeEquals(credentials.Username, expected.Username);
        var passwordMatches = FixedTimeEquals(credentials.Password, expected.Password);
        return usernameMatches & passwordMatches;
    }

    private AuthenticationTicket CmsClientTicket(BasicCredentials credentials)
    {
        Claim[] claims =
        [
            new(ClaimTypes.NameIdentifier, CmsClientId),
            new(ClaimTypes.Name, credentials.Username),
            new(ClaimTypes.Role, AuthRoles.CmsClient),
        ];
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, Scheme.Name));
        return new AuthenticationTicket(principal, Scheme.Name);
    }

    private static bool FixedTimeEquals(string actual, string expected) =>
        CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(actual),
            Encoding.UTF8.GetBytes(expected)
        );
}
