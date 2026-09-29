using System.Buffers.Text;
using System.Text;
using CmsApi.Core.ContentEntities.Listing;
using FluentAssertions;

namespace CmsApi.UnitTests.ContentEntities.Listing;

public sealed class ContentEntityCursorTests
{
    [Test]
    public void Cursor_WithColonInId()
    {
        var cursor = new ContentEntityCursor(UpdatedAt, "article:1");

        var decoded = ContentEntityCursor.TryDecode(cursor.Encode(), out var roundTripped);

        decoded.Should().BeTrue();
        roundTripped.Should().Be(cursor);
    }

    [TestCase(null)]
    [TestCase("not base64url!")]
    public void Token_WithMalformedEncoding(string? token)
    {
        var decoded = ContentEntityCursor.TryDecode(token, out var cursor);

        decoded.Should().BeFalse();
        cursor.Should().BeNull();
    }

    [TestCase("638000000000000000")] // no separator
    [TestCase("yesterday:article-1")] // non-numeric ticks
    [TestCase("638000000000000000:")] // empty id
    public void Token_WithMalformedContent(string content)
    {
        var decoded = ContentEntityCursor.TryDecode(Base64UrlOf(content), out var cursor);

        decoded.Should().BeFalse();
        cursor.Should().BeNull();
    }

    private static readonly DateTimeOffset UpdatedAt = new(2026, 9, 29, 10, 0, 0, TimeSpan.Zero);

    private static string Base64UrlOf(string content) =>
        Base64Url.EncodeToString(Encoding.UTF8.GetBytes(content));
}
