using CmsApi.Auth;
using CmsApi.Auth.AuthorizeCaller;
using CmsApi.Core.Domain.ContentEntities;
using CmsApi.Core.Domain.ContentEntities.Listing;
using CmsApi.Core.Domain.Users;
using CmsApi.Dtos;
using CmsApi.Errors;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CmsApi.Controllers;

/// <summary>Content Entities for Users and Admins. Reads from the reader.</summary>
[ApiController]
[Route("entities")]
[Authorize(Policy = AuthNames.ApiUserPolicy)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
public sealed class ContentEntitiesController(
    [FromKeyedServices(UserRole.User)] IContentEntityReadRepository userReader,
    [FromKeyedServices(UserRole.Admin)] IContentEntityReadRepository adminReader
) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<ContentEntityPageResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
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
    [ProducesResponseType<ContentEntityResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAsync(string id, CancellationToken cancellationToken)
    {
        var contentEntity = await ReaderForUser().FindAsync(id, cancellationToken);
        return contentEntity is null
            ? NotFound(ContentEntityProblems.NotFound())
            : Ok(contentEntity);
    }

    private IContentEntityReadRepository ReaderForUser() =>
        User.UserRole() == UserRole.Admin ? adminReader : userReader;
}
