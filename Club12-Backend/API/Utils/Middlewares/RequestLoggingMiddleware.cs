using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace API.Utils.Middlewares;

public class RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
{
    private const string LogTemplate = @"
───────────────────────────────────────────────

[{Timestamp1} INF]
───────────────────────────────────────────────
HTTP REQUEST LOG [{Timestamp2}]
───────────────────────────────────────────────
Method:        {Method}
Path:          {Path}
Full URL:      {FullUrl}
CorrelationId: {CorrelationId}

Query Parameters:
{QueryParams}

Body:
{Body}

───────────────────────────────────────────────
";

    public async Task InvokeAsync(HttpContext context)
    {
        System.Diagnostics.Stopwatch stopwatch = System.Diagnostics.Stopwatch.StartNew();

        string method = context.Request.Method;
        string url = context.Request.Path + context.Request.QueryString;
        string protocol = context.Request.Protocol;
        string scheme = context.Request.Scheme;
        string host = context.Request.Host.ToString();
        string fullUrl = $"{scheme}://{host}{url}";
        string correlationId = context.TraceIdentifier;

        string queryParams = context.Request.Query.Any()
            ? string.Join(Environment.NewLine, context.Request.Query.Select(q => $"  {q.Key}: {q.Value}"))
            : "  (none)";

        string body = "";
        if (context.Request.ContentLength > 0 && context.Request.Body.CanSeek)
        {
            context.Request.Body.Position = 0;
            using var reader = new StreamReader(context.Request.Body, leaveOpen: true);
            body = await reader.ReadToEndAsync();
            context.Request.Body.Position = 0;
        }

        await next(context);

        stopwatch.Stop();

        string timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffffffZ");

        logger.LogInformation(
            LogTemplate,
            timestamp,
            timestamp,
            method,
            context.Request.Path,
            fullUrl,
            correlationId,
            queryParams,
            body);
    }
}
