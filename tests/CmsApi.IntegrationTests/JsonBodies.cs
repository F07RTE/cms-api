using System.Text.Json;

namespace CmsApi.IntegrationTests;

public static class JsonBodies
{
    /// <summary>The property names of a JSON object, to assert a DTO exposes no more than it should.</summary>
    public static IEnumerable<string> PropertyNames(this JsonElement jsonObject) =>
        jsonObject.EnumerateObject().Select(property => property.Name);
}
