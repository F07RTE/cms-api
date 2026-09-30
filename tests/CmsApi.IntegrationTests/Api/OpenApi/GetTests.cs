using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CmsApi.ApiDocs;
using FluentAssertions;

namespace CmsApi.IntegrationTests.Api.OpenApi;

public sealed class GetTests : IntegrationTest
{
    [Test]
    public async Task AnonymousUser_WithApiDocsEnabled()
    {
        using var client = Orchestrator.CreateClient();

        var response = await client.GetAsync(Orchestrator.OpenApiDocumentRoute);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var document = await response.Content.ReadFromJsonAsync<JsonElement>();
        document
            .GetProperty("paths")
            .PropertyNames()
            .Should()
            .BeEquivalentTo(
                "/cms/events",
                "/entities",
                "/entities/{id}",
                "/entities/{id}/disable",
                "/entities/{id}/enable"
            );
        var scheme = document
            .GetProperty("components")
            .GetProperty("securitySchemes")
            .EnumerateObject()
            .Should()
            .ContainSingle()
            .Subject.Value;
        scheme.GetProperty("type").GetString().Should().Be("http");
        scheme.GetProperty("scheme").GetString().Should().Be("basic");
        var example = Operation(document, "/cms/events", "post")
            .GetProperty("requestBody")
            .GetProperty("content")
            .GetProperty("application/json")
            .GetProperty("example");
        example
            .EnumerateArray()
            .Select(cmsEvent => cmsEvent.GetProperty("type").GetString())
            .Should()
            .Equal("publish", "unPublish", "delete");
    }

    [Test]
    public async Task CmsClient_WithApiDocsDisabled()
    {
        using var client = Orchestrator.WithBasicAuth(
            Orchestrator.CreateClientWithSetting(EnabledKey, bool.FalseString),
            Orchestrator.CmsClientCredentials()
        );

        var response = await client.GetAsync(Orchestrator.OpenApiDocumentRoute);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private const string EnabledKey =
        $"{ApiDocsOptions.SectionName}:{nameof(ApiDocsOptions.Enabled)}";

    private static JsonElement Operation(JsonElement document, string path, string method) =>
        document.GetProperty("paths").GetProperty(path).GetProperty(method);
}
