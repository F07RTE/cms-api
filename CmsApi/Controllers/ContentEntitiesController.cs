using CmsApi.Auth;
using CmsApi.Core.ContentEntities;
using CmsApi.Core.ContentEntities.Listing;
using CmsApi.Core.Users;
using CmsApi.Dtos;
using CmsApi.Errors;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CmsApi.Controllers;

/// <summary>Content Entities for Users and Admins. Reads from the reader.</summary>
[ApiController]
[Route("entities")]
[Authorize(Policy = AuthNames.ApiUserPolicy)]
public sealed class ContentEntitiesController(
    [FromKeyedServices(UserRole.User)] IContentEntityReader userReader,
    [FromKeyedServices(UserRole.Admin)] IContentEntityReader adminReader
) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> ListAsync(
        [FromQuery] ContentEntityPageQueryString query,
        CancellationToken cancellationToken
    )
    {
        var request = ContentEntityPageRequest.Parse(query.Limit, query.Cursor);
        var page = await ReaderForUser().ReadPageAsync(request, cancellationToken);
        return Ok(new ContentEntityPageResponse(page.Items, page.Next?.Encode()));
    }

    // Hidden, unknown and deleted all answer 404, so a User can't tell which one it was.
    [HttpGet("{id}")]
    public async Task<IActionResult> GetAsync(string id, CancellationToken cancellationToken)
    {
        var contentEntity = await ReaderForUser().FindAsync(id, cancellationToken);
        return contentEntity is null
            ? NotFound(ContentEntityProblems.NotFound())
            : Ok(contentEntity);
    }

    private IContentEntityReader ReaderForUser() =>
        User.UserRole() == UserRole.Admin ? adminReader : userReader;
}
