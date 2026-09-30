namespace CmsApi.Dtos;

public sealed record BatchAcceptedResponse(long BatchId, int EventCount, DateTimeOffset ReceivedAt);
