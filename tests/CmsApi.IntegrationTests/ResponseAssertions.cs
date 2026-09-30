using System.Net;
using System.Net.Http.Json;
using System.Net.Mime;
using System.Text.Json;
using FluentAssertions;

namespace CmsApi.IntegrationTests;

public static class ResponseAssertions
{
    public static async Task ShouldBeProblemAsync(
        this HttpResponseMessage response,
        HttpStatusCode status
    )
    {
        response.StatusCode.Should().Be(status);
        response
            .Content.Headers.ContentType?.MediaType.Should()
            .Be(MediaTypeNames.Application.ProblemJson);

        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        problem.GetProperty("status").GetInt32().Should().Be((int)status);
        problem.GetProperty("action").GetString().Should().NotBeNullOrWhiteSpace();
        problem.PropertyNames().Should().Contain(["type", "title", "traceId"]);
    }

    public static async Task ShouldBeChallengedAsync(this HttpResponseMessage response)
    {
        await response.ShouldBeProblemAsync(HttpStatusCode.Unauthorized);
        response
            .Headers.WwwAuthenticate.Should()
            .ContainSingle()
            .Which.ToString()
            .Should()
            .Be(BasicChallenge);
    }

    public static IEnumerable<string> PropertyNames(this JsonElement jsonObject) =>
        jsonObject.EnumerateObject().Select(property => property.Name);

    private const string BasicChallenge = "Basic realm=\"cms-api\", charset=\"UTF-8\"";
}
