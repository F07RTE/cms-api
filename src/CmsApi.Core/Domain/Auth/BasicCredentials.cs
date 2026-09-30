using System.Diagnostics.CodeAnalysis;
using CmsApi.Core.Text;

namespace CmsApi.Core.Domain.Auth;

public sealed record BasicCredentials(string Username, string Password)
{
    public const string Scheme = "Basic";

    private const string SchemePrefix = Scheme + " ";
    private const char Separator = ':';

    public static bool TryParse(
        string? headerValue,
        [NotNullWhen(true)] out BasicCredentials? credentials
    )
    {
        credentials = null;
        if (headerValue?.StartsWith(SchemePrefix, StringComparison.OrdinalIgnoreCase) != true)
        {
            return false;
        }

        var decoded = TryDecode(headerValue[SchemePrefix.Length..].Trim());
        var separatorAt = decoded?.IndexOf(Separator) ?? -1;
        if (decoded is null || separatorAt < 0)
        {
            return false;
        }

        credentials = new BasicCredentials(decoded[..separatorAt], decoded[(separatorAt + 1)..]);
        return true;
    }

    private static string? TryDecode(string token)
    {
        var bytes = new byte[token.Length];
        return Convert.TryFromBase64String(token, bytes, out var length)
            ? StrictUtf8.TryDecode(bytes.AsSpan(0, length))
            : null;
    }

    // Never print the password, e.g. in a log line or an assertion message.
    public override string ToString() =>
        $"{nameof(BasicCredentials)} {{ {nameof(Username)} = {Username} }}";
}
