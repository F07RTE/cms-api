using System.Net;
using System.Net.Http.Json;
using System.Net.Mime;
using System.Text.Json;
using FluentAssertions;

namespace CmsApi.IntegrationTests;

public static class ProblemResponses
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
}
