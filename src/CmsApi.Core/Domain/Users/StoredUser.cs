namespace CmsApi.Core.Domain.Users;

/// <summary>A User as stored: what a login is checked against.</summary>
/// <param name="PasswordHash">Argon2id PHC string.</param>
public sealed record StoredUser(Guid Id, string Username, string PasswordHash, UserRole Role)
{
    // Never print the hash, e.g. in a log line or an assertion message.
    public override string ToString() =>
        $"{nameof(StoredUser)} {{ {nameof(Id)} = {Id}, {nameof(Username)} = {Username}, {nameof(Role)} = {Role} }}";
}
