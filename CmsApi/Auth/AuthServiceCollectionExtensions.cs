using CmsApi.Core.Users;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;

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

        services.AddMemoryCache();
        services.AddSingleton<IPasswordHasher<StoredUser>, Argon2PasswordHasher>();
        services.AddScoped<UserAuthenticator>();
        services
            .AddAuthentication(AuthNames.BasicScheme)
            .AddScheme<AuthenticationSchemeOptions, BasicAuthenticationHandler>(
                AuthNames.BasicScheme,
                configureOptions: null
            );
        services
            .AddAuthorizationBuilder()
            .AddPolicy(
                AuthNames.CmsClientPolicy,
                policy => policy.RequireRole(AuthNames.CmsClientRole)
            )
            .AddPolicy(
                AuthNames.ApiUserPolicy,
                policy => policy.RequireRole(AuthNames.DefaultUserRole, AuthNames.AdminRole)
            )
            .AddPolicy(AuthNames.AdminOnlyPolicy, policy => policy.RequireRole(AuthNames.AdminRole))
            .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());
        return services;
    }
}
