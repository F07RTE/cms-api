using Microsoft.AspNetCore.Mvc;

namespace CmsApi.Errors;

public static class ContentEntityProblems
{
    private const string NotFoundDetail = "No Content Entity with this id.";
    private const string NotFoundAction =
        "Check the id. Deleted Content Entities are gone for good.";

    // The factory adds type, title and traceId, as IProblemDetailsService does for the other errors.
    public static NotFoundObjectResult ContentEntityNotFound(this ControllerBase controller)
    {
        var problem = controller.ProblemDetailsFactory.CreateProblemDetails(
            controller.HttpContext,
            StatusCodes.Status404NotFound,
            detail: NotFoundDetail
        );
        problem.Extensions[Problems.ActionKey] = NotFoundAction;
        return controller.NotFound(problem);
    }
}
