namespace CmsApi.Dtos;

/// <summary>One page of <c>GET /entities</c>. <see cref="NextCursor"/> is null on the last page.</summary>
public sealed record ContentEntityPageResponse<T>(IReadOnlyList<T> Items, string? NextCursor);
