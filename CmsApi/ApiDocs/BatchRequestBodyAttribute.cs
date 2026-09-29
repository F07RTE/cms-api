namespace CmsApi.ApiDocs;

/// <summary>
/// Marks the action that reads a raw Batch from the body, so the OpenAPI document shows its
/// request body and an example. Model binding never sees that body, so nothing else would.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class BatchRequestBodyAttribute : Attribute;
