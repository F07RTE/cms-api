using CmsApi.Core.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace CmsApi.Errors;

public sealed partial class CmsExceptionHandler(
    IProblemDetailsService problemDetails,
    ILogger<CmsExceptionHandler> logger
) : IExceptionHandler
{
    private const string UnexpectedDetail = "An unexpected error occurred.";
    private const string UnexpectedAction =
        "Try again later. If it keeps failing, contact support.";

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken
    )
    {
        var status = StatusCodeFor(exception);
        Log(exception, status);

        httpContext.Response.StatusCode = status;
        return await problemDetails.TryWriteAsync(
            new ProblemDetailsContext
            {
                HttpContext = httpContext,
                ProblemDetails = ProblemFor(exception, status),
                Exception = exception,
            }
        );
    }

    private static int StatusCodeFor(Exception exception) =>
        exception switch
        {
            InvalidBatchException or InvalidPageRequestException => StatusCodes.Status400BadRequest,
            BatchTooLargeException => StatusCodes.Status413PayloadTooLarge,
            _ => StatusCodes.Status500InternalServerError,
        };

    private static ProblemDetails ProblemFor(Exception exception, int status) =>
        exception is CmsApiException expected
            ? Problems.Create(status, expected.Message, expected.Action)
            : Problems.Create(status, UnexpectedDetail, UnexpectedAction);

    // Size and reason only: the body may hold anything and is never logged.
    private void Log(Exception exception, int status)
    {
        if (exception is BatchRejectedException rejected)
        {
            LogBatchRejected(status, rejected.Message, rejected.BodyBytes);
            return;
        }

        LogUnexpected(exception);
    }

    [LoggerMessage(
        LogLevel.Warning,
        "Batch rejected with {StatusCode}: {Reason} ({BodyBytes} bytes)"
    )]
    private partial void LogBatchRejected(int statusCode, string reason, long bodyBytes);

    [LoggerMessage(LogLevel.Error, "Unhandled exception")]
    private partial void LogUnexpected(Exception exception);
}
