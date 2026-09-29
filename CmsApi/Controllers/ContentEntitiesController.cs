using CmsApi.Auth;
using CmsApi.Core.Users;
using CmsApi.Data;
using CmsApi.Data.ContentEntities;
using CmsApi.Dtos;
using CmsApi.Errors;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CmsApi.Controllers;

/// <summary>Content Entities for Users and Admins. Reads from the reader.</summary>
[ApiController]
[Route("entities")]
[Authorize(Policy = AuthNames.ApiUserPolicy)]
public sealed class ContentEntitiesController(ReadDbContext reader) : ControllerBase
{
    private const string NotFoundDetail = "No Content Entity with this id.";
    private const string NotFoundAction =
        "Check the id. Deleted Content Entities are gone for good.";

    // Hidden, unknown and deleted all answer 404, so a User can't tell which one it was.
    [HttpGet("{id}")]
    public async Task<IActionResult> GetAsync(string id, CancellationToken cancellationToken)
    {
        var role = User.UserRole();
        var contentEntity = await reader.FindVisibleContentEntityAsync(id, role, cancellationToken);
        if (contentEntity is null)
        {
            return NotFound(
                Problems.Create(StatusCodes.Status404NotFound, NotFoundDetail, NotFoundAction)
            );
        }

        return role == UserRole.Admin
            ? Ok(AdminContentEntityResponse.From(contentEntity))
            : Ok(ContentEntityResponse.From(contentEntity));
    }
}
