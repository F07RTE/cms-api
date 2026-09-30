using System.ComponentModel.DataAnnotations;

namespace CmsApi.Dtos;

// Strings, empty ones kept, so any bad value reaches Core and answers 400 ProblemDetails with an action.
public sealed class ContentEntityPageQueryString
{
    [DisplayFormat(ConvertEmptyStringToNull = false)]
    public string? Limit { get; init; }

    [DisplayFormat(ConvertEmptyStringToNull = false)]
    public string? Cursor { get; init; }
}
