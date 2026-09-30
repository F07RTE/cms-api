namespace CmsApi.ApiDocs;

/// <summary>Whether the OpenAPI document and the Scalar UI are served. Off unless configured.</summary>
public sealed class ApiDocsOptions
{
    public const string SectionName = "ApiDocs";

    public bool Enabled { get; set; }
}
