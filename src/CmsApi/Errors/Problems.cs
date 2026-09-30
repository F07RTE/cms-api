using Microsoft.AspNetCore.Mvc;

namespace CmsApi.Errors;

public static class Problems
{
    public const string ActionKey = "action";

    public static ProblemDetails Create(int status, string detail, string action) =>
        new()
        {
            Status = status,
            Detail = detail,
            Extensions = { [ActionKey] = action },
        };
}
