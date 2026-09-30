using Microsoft.AspNetCore.Authorization;

namespace CmsApi.Auth.AuthorizeCaller;

public static class AuthPolicies
{
    // An endpoint with no policy still needs an authenticated caller.
    public static IServiceCollection AddCmsAuthPolicies(this IServiceCollection services)
    {
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
