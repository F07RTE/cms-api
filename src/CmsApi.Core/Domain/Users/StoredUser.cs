namespace CmsApi.Core.Domain.Users;

public sealed record StoredUser(Guid Id, string Username, string PasswordHash, UserRole Role)
{
    // Never print the hash, e.g. in a log line or an assertion message.
    public override string ToString() =>
        $"{nameof(StoredUser)} {{ {nameof(Id)} = {Id}, {nameof(Username)} = {Username}, {nameof(Role)} = {Role} }}";
}
