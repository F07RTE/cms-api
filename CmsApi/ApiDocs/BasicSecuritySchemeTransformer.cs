using CmsApi.Auth;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace CmsApi.ApiDocs;

/// <summary>Declares Basic auth for the whole document, so the UI asks for credentials once.</summary>
public sealed class BasicSecuritySchemeTransformer : IOpenApiDocumentTransformer
{
    // The HTTP auth scheme name as OpenAPI spells it (RFC 7617), lower case.
    private const string HttpBasicScheme = "basic";

    private const string Description =
        "The CMS Client credential for POST /cms/events; a User or Admin for /entities.";

    public Task TransformAsync(
        OpenApiDocument document,
        OpenApiDocumentTransformerContext context,
        CancellationToken cancellationToken
    )
    {
        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes = new Dictionary<string, IOpenApiSecurityScheme>
        {
            [AuthNames.BasicScheme] = new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = HttpBasicScheme,
                Description = Description,
            },
        };
        document.Security =
        [
            new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference(AuthNames.BasicScheme, document)] = [],
            },
        ];
        return Task.CompletedTask;
    }
}
