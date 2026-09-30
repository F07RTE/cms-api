using CmsApi.Auth.AuthenticateCmsClient;
using CmsApi.Auth.AuthenticateUser;
using CmsApi.Auth.AuthorizeCaller;
using CmsApi.Core.Domain.Users;
using Microsoft.AspNetCore.Authentication;
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
        services.AddSingleton<CmsClientAuthenticator>();

        services.AddMemoryCache();
        services.AddSingleton<IPasswordHasher<StoredUser>, Argon2PasswordHasher>();
        services.AddScoped<UserAuthenticator>();
        services
            .AddAuthentication(AuthNames.BasicScheme)
            .AddScheme<AuthenticationSchemeOptions, BasicAuthenticationHandler>(
                AuthNames.BasicScheme,
                configureOptions: null
            );
        services.AddCmsAuthPolicies();
        return services;
    }
}
