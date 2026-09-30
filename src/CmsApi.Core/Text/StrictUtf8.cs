using System.Text;

namespace CmsApi.Core.Text;

// Rejects an invalid byte instead of silently turning it into U+FFFD.
public static class StrictUtf8
{
    private static readonly UTF8Encoding Encoding = new(
        encoderShouldEmitUTF8Identifier: false,
        throwOnInvalidBytes: true
    );

    public static string? TryDecode(ReadOnlySpan<byte> bytes)
    {
        try
        {
            return Encoding.GetString(bytes);
        }
        catch (DecoderFallbackException)
        {
            return null;
        }
    }
}
