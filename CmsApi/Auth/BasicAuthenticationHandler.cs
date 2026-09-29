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

/// <summary>Basic auth. Checks the CMS Client credential first, then the Users.</summary>
public sealed class BasicAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    IOptions<CmsCredentials> cmsCredentials,
    UserAuthenticator userAuthenticator,
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
    private const string ForbiddenDetail = "These credentials may not use this endpoint.";
    private const string ForbiddenAction =
        "Call this endpoint with the credentials of a caller allowed to use it.";

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        string? header = Request.Headers.Authorization;
        if (header is null)
        {
            return AuthenticateResult.NoResult();
        }

        var ticket = BasicCredentials.TryParse(header, out var credentials)
            ? await TicketForAsync(header, credentials)
            : null;
        return ticket is null
            ? AuthenticateResult.Fail(InvalidCredentials)
            : AuthenticateResult.Success(ticket);
    }

    private async Task<AuthenticationTicket?> TicketForAsync(
        string header,
        BasicCredentials credentials
    )
    {
        if (IsCmsClient(credentials))
        {
            return CmsClientTicket(credentials);
        }

        var user = await userAuthenticator.AuthenticateAsync(
            header,
            credentials,
            Context.RequestAborted
        );
        return user is null ? null : UserTicket(user);
    }

    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        Response.Headers.WWWAuthenticate = Challenge;
        return WriteProblemAsync(
            StatusCodes.Status401Unauthorized,
            ChallengeDetail,
            ChallengeAction
        );
    }

    protected override Task HandleForbiddenAsync(AuthenticationProperties properties) =>
        WriteProblemAsync(StatusCodes.Status403Forbidden, ForbiddenDetail, ForbiddenAction);

    private async Task WriteProblemAsync(int status, string detail, string action)
    {
        Response.StatusCode = status;
        await problemDetails.WriteAsync(
            new ProblemDetailsContext
            {
                HttpContext = Context,
                ProblemDetails = Problems.Create(status, detail, action),
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

    private AuthenticationTicket CmsClientTicket(BasicCredentials credentials) =>
        Ticket(CmsClientId, credentials.Username, AuthNames.CmsClientRole);

    private AuthenticationTicket UserTicket(AuthenticatedUser user) =>
        Ticket(user.Id.ToString(), user.Username, user.Role.ToString());

    private AuthenticationTicket Ticket(string id, string username, string role)
    {
        Claim[] claims =
        [
            new(ClaimTypes.NameIdentifier, id),
            new(ClaimTypes.Name, username),
            new(ClaimTypes.Role, role),
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
