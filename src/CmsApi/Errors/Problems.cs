using Microsoft.AspNetCore.Mvc;

namespace CmsApi.Errors;

/// <summary>Builds ProblemDetails with the <c>action</c> extension every error carries.</summary>
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
