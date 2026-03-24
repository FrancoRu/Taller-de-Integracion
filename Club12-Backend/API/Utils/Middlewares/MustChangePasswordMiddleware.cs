using Microsoft.AspNetCore.Http;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace API.Utils.Middlewares;

public class MustChangePasswordMiddleware(RequestDelegate next)
{
    private static readonly string[] AllowedPaths =
    [
        "/api/users/", 
        "/api/auth/logout",
    ];

    public async Task InvokeAsync(HttpContext context)
    {
        bool mustChange = context.User.FindFirstValue("must_change_password") == "true";

        if (mustChange && !AllowedPaths.Any(p => context.Request.Path.StartsWithSegments(p)))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(new
            {
                error  = "Password change required.",
                action = "PUT /api/users/{your-id}/password"
            });
            return;
        }

        await next(context);
    }
}
