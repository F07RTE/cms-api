using System.Net.Mime;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace CmsApi.ApiDocs;

public sealed class BatchRequestBodyTransformer : IOpenApiOperationTransformer
{
    private const string Description =
        "A Batch: a JSON array of 1-1000 CMS Events, at most 10 MB. Answers 202 once stored; "
        + "the worker applies it later.";

    private const string ExampleBatch = """
        [
          { "type": "publish", "id": "article-1", "version": 1, "timestamp": "2026-09-29T10:00:00Z", "payload": { "title": "Hello" } },
          { "type": "unPublish", "id": "article-2", "version": 3, "timestamp": "2026-09-29T10:01:00Z", "payload": { "title": "Old news" } },
          { "type": "delete", "id": "article-3", "timestamp": "2026-09-29T10:02:00Z" }
        ]
        """;

    public Task TransformAsync(
        OpenApiOperation operation,
        OpenApiOperationTransformerContext context,
        CancellationToken cancellationToken
    )
    {
        if (
            context
                .Description.ActionDescriptor.EndpointMetadata.OfType<BatchRequestBodyAttribute>()
                .Any()
        )
        {
            operation.RequestBody = BatchRequestBody();
        }

        return Task.CompletedTask;
    }

    private static OpenApiRequestBody BatchRequestBody() =>
        new()
        {
            Required = true,
            Description = Description,
            Content = new Dictionary<string, OpenApiMediaType>
            {
                [MediaTypeNames.Application.Json] = new()
                {
                    Schema = new OpenApiSchema
                    {
                        Type = JsonSchemaType.Array,
                        Items = new OpenApiSchema { Type = JsonSchemaType.Object },
                    },
                    Example = JsonNode.Parse(ExampleBatch),
                },
            },
        };
}
