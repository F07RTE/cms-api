using System.Text;

namespace CmsApi.Core.Text;

/// <summary>UTF-8 that rejects an invalid byte instead of silently turning it into U+FFFD.</summary>
public static class StrictUtf8
{
    private static readonly UTF8Encoding Encoding = new(
        encoderShouldEmitUTF8Identifier: false,
        throwOnInvalidBytes: true
    );

    /// <summary>The decoded text, or null when the bytes are not valid UTF-8.</summary>
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
