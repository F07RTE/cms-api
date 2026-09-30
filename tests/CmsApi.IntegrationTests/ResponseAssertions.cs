using System.Net;
using System.Net.Http.Json;
using System.Net.Mime;
using System.Text.Json;
using FluentAssertions;

namespace CmsApi.IntegrationTests;

public static class ResponseAssertions
{
    /// <summary>Asserts a ProblemDetails body with the status and an <c>action</c>.</summary>
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
    }

    /// <summary>Asserts a 401 ProblemDetails that asks for Basic credentials.</summary>
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

    /// <summary>The property names of a JSON object, to assert a DTO exposes no more than it should.</summary>
    public static IEnumerable<string> PropertyNames(this JsonElement jsonObject) =>
        jsonObject.EnumerateObject().Select(property => property.Name);

    private const string BasicChallenge = "Basic realm=\"cms-api\", charset=\"UTF-8\"";
}
