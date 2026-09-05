using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

using System.Diagnostics;
using System.Threading.Tasks;

namespace API.Utils.Middlewares;

/// <summary>
/// Logs one compact, structured line per HTTP request: method, path, status code, elapsed time, and correlation id.
/// </summary>
public class RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
{
    private const string LogTemplate =
        "HTTP {Method} {PathAndQuery} responded {StatusCode} in {ElapsedMs} ms [{CorrelationId}]";
    private const string HttpMethodOptions = "OPTIONS";

    /// <summary>
    /// Times the request, invokes the pipeline, and logs the outcome at a level matched to the response status code.
    /// </summary>
    /// <param name="context">The current HTTP context.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task InvokeAsync(HttpContext context)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();

        await next(context);

        stopwatch.Stop();

        bool isPreflight = context.Request.Method == HttpMethodOptions;
        int statusCode = context.Response.StatusCode;
        LogLevel level = statusCode switch
        {
            _ when isPreflight => LogLevel.Debug,
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
