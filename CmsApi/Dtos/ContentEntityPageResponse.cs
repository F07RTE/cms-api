namespace CmsApi.Dtos;

/// <summary>One page of <c>GET /entities</c>. <see cref="NextCursor"/> is null on the last page.</summary>
/// <remarks>Items typed <c>object</c>, so each is written as its own DTO with all its fields.</remarks>
public sealed record ContentEntityPageResponse(IReadOnlyList<object> Items, string? NextCursor);
