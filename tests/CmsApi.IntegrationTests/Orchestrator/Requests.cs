using System.Net.Http.Headers;
using System.Net.Mime;
using System.Text;
using System.Text.Json;
using CmsApi.Auth.AuthenticateCmsClient;
using CmsApi.Core.Domain.Auth;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace CmsApi.IntegrationTests;

public static partial class Orchestrator
{
    public const string ContentEntitiesRoute = "/entities";
    public const string OpenApiDocumentRoute = "/openapi/v1.json";
    public const string ScalarRoute = "/scalar";

    private const string BatchRoute = "/cms/events";

    public static HttpClient CreateClient() => Factory.CreateClient();

    public static HttpClient CreateClientWithSetting(string key, string value) =>
        Factory.WithWebHostBuilder(builder => builder.UseSetting(key, value)).CreateClient();

    public static HttpClient WithBasicAuth(HttpClient client, BasicCredentials credentials)
    {
        var token = Convert.ToBase64String(
            Encoding.UTF8.GetBytes($"{credentials.Username}:{credentials.Password}")
        );
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            BasicCredentials.Scheme,
            token
        );
        return client;
    }

    public static BasicCredentials CmsClientCredentials()
    {
        var credentials = Factory.Services.GetRequiredService<IOptions<CmsCredentials>>().Value;
        return new BasicCredentials(credentials.Username, credentials.Password);
    }

    public static object CmsEvent(
        string type,
        string id,
        long version,
        DateTimeOffset timestamp,
        object payload
    ) =>
        new
        {
            type,
            id,
            version,
            timestamp = timestamp.ToString("O"),
            payload,
        };

    public static object DeleteEvent(string id, DateTimeOffset timestamp) =>
        new
        {
            type = "delete",
            id,
            timestamp = timestamp.ToString("O"),
        };

    public static Task<HttpResponseMessage> PostBatchAsync(IEnumerable<object> events) =>
        PostBatchAsync(JsonSerializer.Serialize(events));

    public static Task<HttpResponseMessage> PostBatchAsync(string body) =>
        PostBatchAsync(Encoding.UTF8.GetBytes(body), CmsClientCredentials());

    public static async Task<HttpResponseMessage> PostBatchAsync(
        byte[] body,
        BasicCredentials? credentials
    )
    {
        using var client = credentials is null
            ? CreateClient()
            : WithBasicAuth(CreateClient(), credentials);
        using var content = new ByteArrayContent(body);
        content.Headers.ContentType = new MediaTypeHeaderValue(MediaTypeNames.Application.Json);
        return await client.PostAsync(BatchRoute, content);
    }

    public static async Task<HttpResponseMessage> GetContentEntityAsync(
        string id,
        BasicCredentials credentials
    )
    {
        using var client = WithBasicAuth(CreateClient(), credentials);
        return await client.GetAsync($"{ContentEntitiesRoute}/{id}");
    }

    public static async Task<HttpResponseMessage> ListContentEntitiesAsync(
        BasicCredentials credentials,
        string query = ""
    )
    {
        using var client = WithBasicAuth(CreateClient(), credentials);
        return await client.GetAsync($"{ContentEntitiesRoute}{query}");
    }

    public static Task<HttpResponseMessage> DisableContentEntityAsync(
        string id,
        BasicCredentials credentials
    ) => PatchAsync($"{ContentEntitiesRoute}/{id}/disable", credentials);

    public static Task<HttpResponseMessage> EnableContentEntityAsync(
        string id,
        BasicCredentials credentials
    ) => PatchAsync($"{ContentEntitiesRoute}/{id}/enable", credentials);

    private static async Task<HttpResponseMessage> PatchAsync(
        string route,
        BasicCredentials credentials
    )
    {
        using var client = WithBasicAuth(CreateClient(), credentials);
        return await client.PatchAsync(route, content: null);
    }
}
