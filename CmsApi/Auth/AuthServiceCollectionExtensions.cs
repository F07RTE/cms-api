using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;

namespace CmsApi.Auth;

public static class AuthServiceCollectionExtensions
{
    public static IServiceCollection AddCmsAuth(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        services
            .AddOptions<CmsCredentials>()
            .Bind(configuration.GetSection(CmsCredentials.SectionName))
            .Validate(
                credentials =>
                    credentials.Username.Length
                        is >= CmsCredentials.MinUsernameLength
                            and <= CmsCredentials.MaxUsernameLength,
                $"{CmsCredentials.SectionName}:{nameof(CmsCredentials.Username)} must be "
                    + $"{CmsCredentials.MinUsernameLength}-{CmsCredentials.MaxUsernameLength} characters."
            )
            .Validate(
                credentials => Guid.TryParse(credentials.Password, out _),
                $"{CmsCredentials.SectionName}:{nameof(CmsCredentials.Password)} must be a GUID."
            )
            .ValidateOnStart();

        services
            .AddAuthentication(AuthSchemes.Basic)
            .AddScheme<AuthenticationSchemeOptions, BasicAuthenticationHandler>(
                AuthSchemes.Basic,
                configureOptions: null
            );
        services
            .AddAuthorizationBuilder()
            .AddPolicy(AuthPolicies.CmsClient, policy => policy.RequireRole(AuthRoles.CmsClient))
            .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());
        return services;
    }
}
