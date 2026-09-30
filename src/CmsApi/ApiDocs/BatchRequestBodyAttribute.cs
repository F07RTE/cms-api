namespace CmsApi.ApiDocs;

// The Batch body is read raw, so model binding never sees it; this marker puts it in the OpenAPI document.
[AttributeUsage(AttributeTargets.Method)]
public sealed class BatchRequestBodyAttribute : Attribute;
