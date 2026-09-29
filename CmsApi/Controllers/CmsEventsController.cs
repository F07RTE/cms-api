using CmsApi.Auth;
using CmsApi.Core.Batches;
using CmsApi.Core.Inbox;
using CmsApi.Dtos;
using CmsApi.Http;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CmsApi.Controllers;

/// <summary>The CMS webhook. Stores the Batch in the Inbox; the worker processes it later.</summary>
[ApiController]
[Route("cms/events")]
[Authorize(Policy = AuthPolicies.CmsClient)]
public sealed class CmsEventsController(IInbox inbox) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<BatchAcceptedResponse>> PostAsync(
        CancellationToken cancellationToken
    )
    {
        var body = await Request.ReadBodyAsync(BatchLimits.MaxBodyBytes, cancellationToken);
        var validBody = BatchBodyValidator.Validate(body);
        var batch = await inbox.EnqueueAsync(
            validBody.Text,
            validBody.EventCount,
            cancellationToken
        );
        return Accepted(
            new BatchAcceptedResponse(batch.BatchId, batch.EventCount, batch.ReceivedAt)
        );
    }
}
