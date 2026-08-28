using Application.Interfaces.Maintenance;

using Microsoft.AspNetCore.Http;

using System;
using System.Threading.Tasks;

namespace API.Utils.Middlewares;

/// <summary>
/// Enforces the maintenance lock (HU-92) app-wide: while a backup or restore
/// holds the lock, any mutating request (POST/PUT/DELETE/PATCH) is
/// short-circuited with 503 "operation in progress" so nobody can modify data
/// mid-operation. Read requests (GET/HEAD/OPTIONS) always pass, so the UI can
/// still render and poll the maintenance status. The status endpoint is
/// additionally allow-listed so it is reachable regardless of verb.
/// </summary>
public sealed class MaintenanceModeMiddleware(RequestDelegate next, IMaintenanceState state)
{
    /// <summary>
    /// Path prefixes that stay open even while locked, so the client can read
    /// the maintenance banner state.
    /// </summary>
    private static readonly string[] AllowedPaths =
    [
        "/api/backups/status",
    ];

    public async Task InvokeAsync(HttpContext context)
    {
        if (state.IsActive
            && IsMutating(context.Request.Method)
            && !IsAllowed(context.Request.Path))
        {
            MaintenanceStatus? status = state.Current;

            context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            context.Response.Headers.RetryAfter = "5";
            await context.Response.WriteAsJsonAsync(new
            {
                error = "A maintenance operation is in progress. Please try again shortly.",
                operation = status?.Operation,
                startedAt = status?.StartedAt,
            });
            return;
        }

        await next(context);
    }

    private static bool IsMutating(string method)
    {
        return HttpMethods.IsPost(method)
            || HttpMethods.IsPut(method)
            || HttpMethods.IsDelete(method)
            || HttpMethods.IsPatch(method);
    }

    private static bool IsAllowed(PathString path)
    {
        return Array.Exists(AllowedPaths, p => path.StartsWithSegments(p, StringComparison.OrdinalIgnoreCase));
    }
}
