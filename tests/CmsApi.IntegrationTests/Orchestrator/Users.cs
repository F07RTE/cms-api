using CmsApi.Core.Domain.Auth;
using CmsApi.Core.Domain.Users;
using CmsApi.Data.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CmsApi.IntegrationTests;

/// <summary>Users: seeding and reading back the <c>users</c> table.</summary>
public static partial class Orchestrator
{
    public const string ReaderUsername = "reader";
    public const string AdminUsername = "admin";

    /// <summary>Stores a User with a fresh random password; returns the credentials to log in with.</summary>
    public static async Task<BasicCredentials> CreateUserAsync(string username, UserRole role)
    {
        var hasher = Factory.Services.GetRequiredService<IPasswordHasher<StoredUser>>();
        var password = Guid.NewGuid().ToString();
        var user = new StoredUser(Guid.NewGuid(), username, string.Empty, role);
        await CreateUserAsync(user with { PasswordHash = hasher.HashPassword(user, password) });
        return new BasicCredentials(username, password);
    }

    /// <summary>Stores a User with the given password hash, as-is.</summary>
    public static Task CreateUserAsync(StoredUser user) =>
        WithWriterAsync(context =>
        {
            context.Users.Add(
                new User
                {
                    Id = user.Id,
                    Username = user.Username,
                    PasswordHash = user.PasswordHash,
                    Role = user.Role,
                }
            );
            return context.SaveChangesAsync();
        });

    /// <summary>Removes every User, e.g. to show a login was served from the credential cache.</summary>
    public static Task DeleteUsersAsync() =>
        WithWriterAsync(context => context.Users.ExecuteDeleteAsync());

    public static Task<List<User>> ReadUsersAsync() =>
        WithWriterAsync(context => context.Users.OrderBy(user => user.Username).ToListAsync());
}
