using System.Net;
using System.Net.Mime;
using FluentAssertions;

namespace CmsApi.IntegrationTests.Api.Scalar;

public sealed class GetTests : IntegrationTest
{
    [Test]
    public async Task AnonymousUser_WithApiDocsEnabled()
    {
        using var client = Orchestrator.CreateClient();

        var response = await client.GetAsync(Orchestrator.ScalarRoute);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be(MediaTypeNames.Text.Html);
    }
}
