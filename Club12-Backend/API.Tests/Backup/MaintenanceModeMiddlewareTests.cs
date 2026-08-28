using API.Utils.Middlewares;

using Application.Backup;
using Application.Interfaces.Backup;

using Microsoft.AspNetCore.Http;

namespace API.Tests.Backup;

/// <summary>
/// Unit tests for MaintenanceModeMiddleware (design.md's "Maintenance
/// gate is middleware placed after UseCors, before
/// UseAuthentication" decision — covers every request shape,
/// including unmatched routes and Swagger, unlike an MVC filter). Exercises
/// InvokeAsync directly against a DefaultHttpContext and a real
/// MaintenanceModeState — no HTTP pipeline/host required.
/// </summary>
public class MaintenanceModeMiddlewareTests
{
    private sealed class CallCounter
    {
        public int Count { get; set; }
    }

    private static (MaintenanceModeMiddleware Middleware, CallCounter Counter) CreateSut(IMaintenanceModeState state)
    {
        CallCounter counter = new();
        RequestDelegate next = _ =>
        {
            counter.Count++;
            return Task.CompletedTask;
        };

        return (new MaintenanceModeMiddleware(next, state), counter);
    }

    /// <summary>
    /// threat-matrix "Routing gate bypass (maintenance 503)": /api/backups,
    /// /swagger, and an unmatched route all must return 503 while
    /// maintenance is active — the middleware runs before endpoint routing,
    /// so this must not depend on any route being matched.
    /// </summary>
    [Theory]
    [InlineData("/api/backups")]
    [InlineData("/swagger")]
    [InlineData("/this-route-does-not-exist")]
    public async Task InvokeAsync_MaintenanceActive_NonAllowedPath_Returns503_DoesNotCallNext(string path)
    {
        MaintenanceModeState state = new();
        state.Enter("restore in progress");
        (MaintenanceModeMiddleware sut, CallCounter counter) = CreateSut(state);
        DefaultHttpContext context = new();
        context.Request.Path = path;

        await sut.InvokeAsync(context);

        Assert.Equal(StatusCodes.Status503ServiceUnavailable, context.Response.StatusCode);
        Assert.Equal(0, counter.Count);
    }

    [Theory]
    [InlineData("/health")]
    [InlineData("/health/ready")]
    [InlineData("/api/maintenance")]
    public async Task InvokeAsync_MaintenanceActive_AllowedPath_CallsNext(string path)
    {
        MaintenanceModeState state = new();
        state.Enter("restore in progress");
        (MaintenanceModeMiddleware sut, CallCounter counter) = CreateSut(state);
        DefaultHttpContext context = new();
        context.Request.Path = path;

        await sut.InvokeAsync(context);

        Assert.Equal(1, counter.Count);
        Assert.NotEqual(StatusCodes.Status503ServiceUnavailable, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_MaintenanceInactive_CallsNext_ForAnyPath()
    {
        MaintenanceModeState state = new();
        (MaintenanceModeMiddleware sut, CallCounter counter) = CreateSut(state);
        DefaultHttpContext context = new();
        context.Request.Path = "/api/backups";

        await sut.InvokeAsync(context);

        Assert.Equal(1, counter.Count);
        Assert.NotEqual(StatusCodes.Status503ServiceUnavailable, context.Response.StatusCode);
    }
}
