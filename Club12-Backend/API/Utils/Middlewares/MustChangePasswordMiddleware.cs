using Application.Utils.Constants.Auth;

using Microsoft.AspNetCore.Http;

using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace API.Utils.Middlewares;

/// <summary>
/// Enforces the must-change-password flow: once
/// <see cref="CustomClaimTypes.MustChangePassword"/> is set on the caller's
/// token, every request except the user-update and logout endpoints is
/// rejected with 403 until the password is changed. Registered after
/// UseAuthentication()/UseAuthorization() so the claim has already been
/// resolved (mirrors MaintenanceModeMiddleware's placement pattern).
/// </summary>
public class MustChangePasswordMiddleware(RequestDelegate next)
{
    private static readonly string[] AllowedPaths =
    [
        "/api/users/",
        "/api/auth/logout",
    ];

    public async Task InvokeAsync(HttpContext context)
    {
        bool mustChange = context.User.FindFirstValue(CustomClaimTypes.MustChangePassword) == "true";

        if (mustChange && !Array.Exists(AllowedPaths, p => context.Request.Path.StartsWithSegments(p)))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(new
            {
                error = "Password change required.",
                action = "PUT /api/users/{your-id}/password"
            });
            return;
        }

        await next(context);
    }
}
