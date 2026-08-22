using Application.Interfaces.Backup;

using Microsoft.AspNetCore.Http;

using System;
using System.Threading.Tasks;

namespace API.Utils.Middlewares;

/// <summary>
/// database-restore#Maintenance-Mode-Window: while IMaintenanceModeState
/// is active, every request except the allow-listed paths responds
/// 503 (design.md's "Maintenance gate is middleware placed after
/// UseCors, before UseAuthentication" decision — mirrors
/// MustChangePasswordMiddleware's shape). Registered after
/// UseCors() so the 503 still carries CORS headers, and before
/// UseAuthentication() so the gate is not preempted by an auth
/// failure. Placing this as middleware (not an MVC filter) covers every
/// request shape, including unmatched routes and Swagger, which an
/// IAsyncActionFilter would miss (threat-matrix "Routing gate bypass").
/// The allow-list covers /health (StartsWithSegments also matches
/// /health/ready) and /api/maintenance — the manual escape hatch,
/// which still enforces its own [Authorize(Roles = Admin)] (threat-matrix
/// "Escape-hatch abuse").
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
