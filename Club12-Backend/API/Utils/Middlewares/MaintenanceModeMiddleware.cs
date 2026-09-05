using Application.Interfaces.Backup;

using Microsoft.AspNetCore.Http;

using System;
using System.Threading.Tasks;

namespace API.Utils.Middlewares;

/// <summary>
/// Returns 503 for every request outside the allow-listed paths while maintenance mode is active.
/// </summary>
public class MaintenanceModeMiddleware(RequestDelegate next, IMaintenanceModeState maintenanceModeState)
{
    private static readonly string[] AllowedPaths =
    [
        "/health",
        "/api/maintenance",
    ];

    public async Task InvokeAsync(HttpContext context)
    {
        if (maintenanceModeState.IsActive && !Array.Exists(AllowedPaths, p => context.Request.Path.StartsWithSegments(p)))
        {
            context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            context.Response.Headers.RetryAfter = "30";
            await context.Response.WriteAsJsonAsync(new
            {
                error = "The application is temporarily in maintenance mode.",
                reason = maintenanceModeState.Reason,
            });
            return;
        }

        await next(context);
    }
}
