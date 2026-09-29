using CmsApi.Auth;
using CmsApi.Core.ContentEntities;
using CmsApi.Errors;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CmsApi.Controllers;

/// <summary>An Admin disables or enables a Content Entity. Writes and answers from the writer.</summary>
[ApiController]
[Route("entities/{id}")]
[Authorize(Policy = AuthNames.AdminOnlyPolicy)]
public sealed class ContentEntityOverridesController(IContentEntityOverrides overrides)
    : ControllerBase
{
    [HttpPatch("disable")]
    public async Task<IActionResult> DisableAsync(string id, CancellationToken cancellationToken) =>
        OkOrNotFound(await overrides.DisableAsync(id, User.Username(), cancellationToken));

    [HttpPatch("enable")]
    public async Task<IActionResult> EnableAsync(string id, CancellationToken cancellationToken) =>
        OkOrNotFound(await overrides.EnableAsync(id, cancellationToken));

    private IActionResult OkOrNotFound(IProjectedContentEntity? contentEntity) =>
        contentEntity is null ? NotFound(ContentEntityProblems.NotFound()) : Ok(contentEntity);
}
