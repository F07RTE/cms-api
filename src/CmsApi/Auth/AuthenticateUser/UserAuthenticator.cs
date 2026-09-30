using System.Security.Cryptography;
using System.Text;
using CmsApi.Core.Domain.Auth;
using CmsApi.Core.Domain.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Memory;

namespace CmsApi.Auth.AuthenticateUser;

// Successful checks are cached, so Argon2id runs at most once per header per CacheDuration (ADR 0003).
public sealed class UserAuthenticator(
    IUserRepository users,
    IPasswordHasher<StoredUser> passwordHasher,
    IMemoryCache cache
)
{
    private const string CacheKeyPrefix = "basic-auth:";

    // The hash of a random password nobody knows. Verified for unknown usernames, so the time
    // taken doesn't reveal which usernames exist.
    private const string DummyHash =
        "$argon2id$v=19$m=19456,t=2,p=1$Tihzd6f23Xnfs4f4sUdcBA$IHfO6NB1p/rkIx4L1vT7Msa1dEtMsQld/W/aRAFNHoI";

    private static readonly TimeSpan CacheDuration = TimeSpan.FromSeconds(60);

    private static readonly StoredUser UnknownUser = new(
        Guid.Empty,
        string.Empty,
        DummyHash,
        UserRole.User
    );

    public async Task<AuthenticatedUser?> AuthenticateAsync(
        string authorizationHeader,
        BasicCredentials credentials,
        CancellationToken cancellationToken
    )
    {
        var cacheKey = CacheKey(authorizationHeader);
        if (cache.TryGetValue(cacheKey, out AuthenticatedUser? cached))
        {
            return cached;
        }

        var user = await VerifyAsync(credentials, cancellationToken);
        if (user is not null)
        {
            cache.Set(cacheKey, user, CacheDuration);
        }

        return user;
    }

    private async Task<AuthenticatedUser?> VerifyAsync(
        BasicCredentials credentials,
        CancellationToken cancellationToken
    )
    {
        var user = await users.FindByUsernameAsync(credentials.Username, cancellationToken);
        var verified = user ?? UnknownUser;
        var result = passwordHasher.VerifyHashedPassword(
            verified,
            verified.PasswordHash,
            credentials.Password
        );
        if (user is null || result == PasswordVerificationResult.Failed)
        {
            return null;
        }

        await RehashIfNeededAsync(user, credentials.Password, result, cancellationToken);
        return new AuthenticatedUser(user.Id, user.Username, user.Role);
    }

    private async Task RehashIfNeededAsync(
        StoredUser user,
        string password,
        PasswordVerificationResult result,
        CancellationToken cancellationToken
    )
    {
        if (result == PasswordVerificationResult.SuccessRehashNeeded)
        {
            var passwordHash = passwordHasher.HashPassword(user, password);
            await users.UpdatePasswordHashAsync(user.Id, passwordHash, cancellationToken);
        }
    }

    // A hash of the header: the cache never holds a password.
    private static string CacheKey(string authorizationHeader) =>
        CacheKeyPrefix
        + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(authorizationHeader)));
}
