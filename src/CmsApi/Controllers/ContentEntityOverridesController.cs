using CmsApi.Auth;
using CmsApi.Auth.AuthorizeCaller;
using CmsApi.Core.Domain.ContentEntities;
using CmsApi.Dtos;
using CmsApi.Errors;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CmsApi.Controllers;

[ApiController]
[Route("entities/{id}")]
[Authorize(Policy = AuthNames.AdminOnlyPolicy)]
[ProducesResponseType<AdminContentEntityResponse>(StatusCodes.Status200OK)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
public sealed class ContentEntityOverridesController(IContentEntityRepository contentEntities)
    : ControllerBase
{
    [HttpPatch("disable")]
    public async Task<IActionResult> DisableAsync(string id, CancellationToken cancellationToken) =>
        OkOrNotFound(await contentEntities.DisableAsync(id, User.Username(), cancellationToken));

    [HttpPatch("enable")]
    public async Task<IActionResult> EnableAsync(string id, CancellationToken cancellationToken) =>
        OkOrNotFound(await contentEntities.EnableAsync(id, cancellationToken));

    private IActionResult OkOrNotFound(StoredContentEntity? contentEntity) =>
        contentEntity is null
            ? NotFound(ContentEntityProblems.NotFound())
            : Ok(AdminContentEntityResponse.From(contentEntity));
}
