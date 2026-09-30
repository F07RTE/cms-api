using CmsApi.ApiDocs;
using CmsApi.Auth;
using CmsApi.Core.Domain.Batches;
using CmsApi.Core.UseCases.ReceiveBatch;
using CmsApi.Dtos;
using CmsApi.Http;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CmsApi.Controllers;

[ApiController]
[Route("cms/events")]
[Authorize(Policy = AuthNames.CmsClientPolicy)]
public sealed class CmsEventsController(BatchReceiver batchReceiver) : ControllerBase
{
    [HttpPost]
    [BatchRequestBody]
    [ProducesResponseType<BatchAcceptedResponse>(StatusCodes.Status202Accepted)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status413PayloadTooLarge)]
    public async Task<ActionResult<BatchAcceptedResponse>> PostAsync(
        CancellationToken cancellationToken
    )
    {
        var body = await Request.ReadBodyAsync(BatchLimits.MaxBodyBytes, cancellationToken);
        var batch = await batchReceiver.ReceiveAsync(body, cancellationToken);
        return Accepted(
            new BatchAcceptedResponse(batch.BatchId, batch.EventCount, batch.ReceivedAt)
        );
    }
}
