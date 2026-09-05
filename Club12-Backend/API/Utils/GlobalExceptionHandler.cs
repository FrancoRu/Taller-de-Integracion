using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace API.Utils;

/// <summary>
/// Handles global exceptions and returns standardized ProblemDetails responses.
/// </summary>
public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _logger.LogInformation("GlobalHandlerException initialized.");
    }

    /// <summary>
    /// Tries to handle exceptions globally and returns standardized ProblemDetails.
    /// </summary>
    /// <param name="httpContext">The current HTTP context.</param>
    /// <param name="exception">The exception to handle.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task representing the asynchronous operation of handling the exception.</returns>
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        IExceptionHandlerFeature? exceptionHandlerFeature = httpContext.Features.Get<IExceptionHandlerFeature>();
        if (exceptionHandlerFeature is not null)
        {
            string traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;
            Exception exceptionDetails = exceptionHandlerFeature.Error;

            _logger.LogError(exceptionDetails,
                "An unhandled exception occurred on the {MachineName}. TraceId: {TraceId}.",
                Environment.MachineName,
                traceId);

            (int statusCode, string title, bool exposeMessage) = MapException(exceptionDetails);

            IResult result = Results.Problem(
                title: title,
                detail: exposeMessage
                    ? exceptionDetails.Message
                    : "An unexpected error occurred. Please contact support with the trace ID below.",
                statusCode: statusCode,
                extensions: new Dictionary<string, object?> { ["traceId"] = traceId }
            );

            await result.ExecuteAsync(httpContext);

            return true;
        }

        return false;
    }

    /// <summary>
    /// Maps known exceptions to appropriate HTTP status codes and response titles. The
    /// third value controls whether the exception's own message is safe to return to the
    /// client: true for exceptions whose message describes invalid input or a known
    /// business-state conflict, false for exceptions that may carry internal details
    /// (connection strings, stack state, unclassified failures) that must not leak.
    /// </summary>
    /// <param name="exception">The exception to map.</param>
    /// <returns>
    /// A tuple containing the HTTP status code, a corresponding title for the response,
    /// and whether the exception's message is safe to expose to the client.
    /// </returns>
    private static (int StatusCode, string Title, bool ExposeMessage) MapException(Exception exception)
    {
        return exception switch
        {
            ArgumentNullException => (StatusCodes.Status400BadRequest, "Bad Request: Required argument is missing.", true),
            UnauthorizedAccessException => (StatusCodes.Status403Forbidden, "Forbidden: You do not have permission to access this resource.", true),
            KeyNotFoundException => (StatusCodes.Status404NotFound, "Not Found: The specified resource could not be found.", true),
            InvalidOperationException => (StatusCodes.Status409Conflict, "Conflict: The operation is invalid in the current state.", true),
            TimeoutException => (StatusCodes.Status408RequestTimeout, "Request Timeout: The operation took too long to complete.", false),
            FormatException => (StatusCodes.Status400BadRequest, "Bad Request: Invalid format encountered.", true),
            NotImplementedException => (StatusCodes.Status501NotImplemented, "Not Implemented: The requested functionality is not available.", true),
            StackOverflowException => (StatusCodes.Status500InternalServerError, "Internal Server Error: Stack overflow occurred.", false),
            OperationCanceledException => (StatusCodes.Status499ClientClosedRequest, "Client Closed Request: Operation was cancelled by the client.", true),
            _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred.", false)
        };
    }
}
