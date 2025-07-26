using Microsoft.AspNetCore.Diagnostics;

using System.Diagnostics;

namespace Club12.API.Utils;

/// <summary>
/// Handles global exceptions and returns standardized ProblemDetails responses.
/// </summary>
public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="logger"></param>
    /// <exception cref="ArgumentNullException"></exception>
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

            (int statusCode, string title) = MapException(exceptionDetails);

            IResult result = Results.Problem(
                title: title,
                detail: exceptionDetails.Message,
                statusCode: statusCode,
                extensions: new Dictionary<string, object?> { ["traceId"] = traceId }
            );

            await result.ExecuteAsync(httpContext);

            return true;
        }

        return false;
    }

    /// <summary>
    /// Maps known exceptions to appropriate HTTP status codes and response titles.
    /// </summary>
    /// <param name="exception">The exception to map.</param>
    /// <returns>
    /// A tuple containing the HTTP status code and a corresponding title for the response.
    /// </returns>
    private static (int StatusCode, string Title) MapException(Exception exception) => exception switch
    {
        ArgumentNullException => (StatusCodes.Status400BadRequest, "Bad Request: Required argument is missing."),
        UnauthorizedAccessException => (StatusCodes.Status403Forbidden, "Forbidden: You do not have permission to access this resource."),
        KeyNotFoundException => (StatusCodes.Status404NotFound, "Not Found: The specified resource could not be found."),
        InvalidOperationException => (StatusCodes.Status409Conflict, "Conflict: The operation is invalid in the current state."),
        TimeoutException => (StatusCodes.Status408RequestTimeout, "Request Timeout: The operation took too long to complete."),
        FormatException => (StatusCodes.Status400BadRequest, "Bad Request: Invalid format encountered."),
        NotImplementedException => (StatusCodes.Status501NotImplemented, "Not Implemented: The requested functionality is not available."),
        StackOverflowException => (StatusCodes.Status500InternalServerError, "Internal Server Error: Stack overflow occurred."),
        OperationCanceledException => (StatusCodes.Status499ClientClosedRequest, "Client Closed Request: Operation was cancelled by the client."),
        _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred.")
    };
}
