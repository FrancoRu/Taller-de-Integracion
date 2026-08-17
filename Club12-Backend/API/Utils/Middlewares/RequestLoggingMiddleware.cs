using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace API.Utils.Middlewares;

/// <summary>
/// Middleware that logs one compact, structured line per HTTP request: method,
/// path (including query string), status code, elapsed time, and correlation
/// id. Never logs the request body, since routes like login/register would
/// otherwise write plaintext credentials to the log stream.
/// </summary>
public class RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
{
    private const string LogTemplate =
        "HTTP {Method} {PathAndQuery} responded {StatusCode} in {ElapsedMs} ms [{CorrelationId}]";

    /// <summary>
    /// Times the request, invokes the rest of the pipeline, and logs the
    /// outcome at a level matched to the response status code.
    /// </summary>
    /// <param name="context">The current HTTP context.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task InvokeAsync(HttpContext context)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();

        await next(context);

        stopwatch.Stop();

        int statusCode = context.Response.StatusCode;
        LogLevel level = statusCode switch
        {
            >= 500 => LogLevel.Error,
            >= 400 => LogLevel.Warning,
            _ => LogLevel.Information,
        };

        logger.Log(
            level,
            LogTemplate,
            context.Request.Method,
            context.Request.Path + context.Request.QueryString,
            statusCode,
            stopwatch.ElapsedMilliseconds,
            context.TraceIdentifier);
    }
}
