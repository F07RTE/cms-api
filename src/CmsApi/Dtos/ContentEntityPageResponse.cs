namespace CmsApi.Dtos;

public sealed record ContentEntityPageResponse<T>(IReadOnlyList<T> Items, string? NextCursor);
