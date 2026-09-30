using System.Text;
using CmsApi.Core.Domain.Auth;
using FluentAssertions;

namespace CmsApi.UnitTests.Domain.Auth;

public sealed class BasicCredentialsTests
{
    [Test]
    public void Header_WithColonInPassword()
    {
        var parsed = BasicCredentials.TryParse(Basic("cms-client:pass:word"), out var credentials);

        parsed.Should().BeTrue();
        credentials.Should().Be(new BasicCredentials("cms-client", "pass:word"));
    }

    [TestCase(null)]
    [TestCase("Bearer Y21zOnBhc3M=")]
    [TestCase("Basic not-base64!")]
    [TestCase("Basic Y21zLWNsaWVudA==")] // "cms-client", no colon
    public void Header_WithMalformedValue(string? headerValue)
    {
        var parsed = BasicCredentials.TryParse(headerValue, out var credentials);

        parsed.Should().BeFalse();
        credentials.Should().BeNull();
    }

    private static string Basic(string userPass) =>
        $"Basic {Convert.ToBase64String(Encoding.UTF8.GetBytes(userPass))}";
}
