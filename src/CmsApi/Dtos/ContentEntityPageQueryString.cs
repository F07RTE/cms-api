using System.ComponentModel.DataAnnotations;

namespace CmsApi.Dtos;

/// <summary>
/// The raw <c>?limit=&amp;cursor=</c> of <c>GET /entities</c>. Strings, empty ones kept, so any bad
/// value is parsed by Core and answers 400 ProblemDetails with an action.
/// </summary>
public sealed class ContentEntityPageQueryString
{
    [DisplayFormat(ConvertEmptyStringToNull = false)]
    public string? Limit { get; init; }

    [DisplayFormat(ConvertEmptyStringToNull = false)]
    public string? Cursor { get; init; }
}
