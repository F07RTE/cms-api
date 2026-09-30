using Microsoft.AspNetCore.Mvc;

namespace CmsApi.Errors;

public static class ContentEntityProblems
{
    private const string NotFoundDetail = "No Content Entity with this id.";
    private const string NotFoundAction =
        "Check the id. Deleted Content Entities are gone for good.";

    public static ProblemDetails NotFound() =>
        Problems.Create(StatusCodes.Status404NotFound, NotFoundDetail, NotFoundAction);
}
