using CmsApi.Core.Domain.ContentEntities;
using CmsApi.Core.Domain.Users;
using FluentAssertions;

namespace CmsApi.UnitTests.Domain.ContentEntities;

public sealed class ContentEntityVisibilityTests
{
    [TestCase(true, false, true)]
    [TestCase(false, false, false)]
    [TestCase(true, true, false)]
    public void DefaultUser_WithFlags(bool isPublished, bool isDisabledByAdmin, bool visible)
    {
        var isVisible = ContentEntityVisibility.VisibleTo<Flags>(UserRole.User).Compile();

        isVisible(new Flags(isPublished, isDisabledByAdmin)).Should().Be(visible);
    }

    [Test]
    public void AdminUser_WithUnpublishedDisabledContentEntity()
    {
        var isVisible = ContentEntityVisibility.VisibleTo<Flags>(UserRole.Admin).Compile();

        isVisible(new Flags(IsPublished: false, IsDisabledByAdmin: true)).Should().BeTrue();
    }

    private sealed record Flags(bool IsPublished, bool IsDisabledByAdmin) : IContentEntityFlags;
}
