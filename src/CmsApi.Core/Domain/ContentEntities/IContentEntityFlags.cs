namespace CmsApi.Core.Domain.ContentEntities;

/// <summary>The two flags that decide whether a Content Entity is Visible.</summary>
public interface IContentEntityFlags
{
    bool IsPublished { get; }

    bool IsDisabledByAdmin { get; }
}
