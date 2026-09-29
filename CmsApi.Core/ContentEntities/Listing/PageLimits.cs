namespace CmsApi.Core.ContentEntities.Listing;

/// <summary>How many Content Entities one page of <c>GET /entities</c> holds.</summary>
public static class PageLimits
{
    public const int Default = 50;
    public const int Min = 1;
    public const int Max = 200;
}
