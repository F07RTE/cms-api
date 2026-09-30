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

[ApiController]
[Route("entities")]
[Authorize(Policy = AuthNames.ApiUserPolicy)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
public sealed class ContentEntitiesController(IContentEntityReadRepository contentEntities)
    : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<ContentEntityPageResponse<ContentEntityResponse>>(
        StatusCodes.Status200OK
    )]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ListAsync(
        [FromQuery] ContentEntityPageQueryString query,
        CancellationToken cancellationToken
    )
    {
        var request = ContentEntityPageRequest.Parse(query.Limit, query.Cursor);
        var role = User.UserRole();
        var page = await contentEntities.ReadPageAsync(request, role, cancellationToken);
        var nextCursor = page.Next?.Encode();
        return role == UserRole.Admin
            ? Ok(
                new ContentEntityPageResponse<AdminContentEntityResponse>(
                    [.. page.Items.Select(AdminContentEntityResponse.From)],
                    nextCursor
                )
            )
            : Ok(
                new ContentEntityPageResponse<ContentEntityResponse>(
                    [.. page.Items.Select(ContentEntityResponse.From)],
                    nextCursor
                )
            );
    }

    // Hidden, unknown and deleted all answer 404, so a User can't tell which one it was.
    [HttpGet("{id}")]
    [ProducesResponseType<ContentEntityResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAsync(string id, CancellationToken cancellationToken)
    {
        var role = User.UserRole();
        var contentEntity = await contentEntities.FindAsync(id, role, cancellationToken);
        if (contentEntity is null)
        {
            return NotFound(ContentEntityProblems.NotFound());
        }

        return role == UserRole.Admin
            ? Ok(AdminContentEntityResponse.From(contentEntity))
            : Ok(ContentEntityResponse.From(contentEntity));
    }
}
