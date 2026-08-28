using API.Utils.Middlewares;

using Application.Maintenance;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace API.Tests.Backup;

/// <summary>
/// Unit tests for the HU-92 maintenance middleware: while locked it
/// short-circuits mutating requests with 503 and never calls the next
/// delegate; reads pass through; when unlocked everything passes; and the
/// status endpoint stays reachable even while locked.
/// </summary>
public class MaintenanceModeMiddlewareTests
{
    private static DefaultHttpContext BuildContext(string method, string path)
    {
        DefaultHttpContext ctx = new();
        ctx.Request.Method = method;
        ctx.Request.Path = path;
        ctx.Response.Body = new MemoryStream();
        ctx.RequestServices = new ServiceCollection().BuildServiceProvider();
        return ctx;
    }

    [Theory]
    [InlineData("POST")]
    [InlineData("PUT")]
    [InlineData("DELETE")]
    [InlineData("PATCH")]
    public async Task Locked_MutatingRequest_ShortCircuitsWith503_NextNotCalled(string method)
    {
        MaintenanceState state = new();
        using IDisposable lease = state.Enter("backup");
        bool nextCalled = false;
        MaintenanceModeMiddleware middleware = new(_ => { nextCalled = true; return Task.CompletedTask; }, state);

        DefaultHttpContext ctx = BuildContext(method, "/api/tournaments");
        await middleware.InvokeAsync(ctx);

        Assert.Equal(StatusCodes.Status503ServiceUnavailable, ctx.Response.StatusCode);
        Assert.False(nextCalled, "A mutating request must not reach the pipeline while locked.");
    }

    [Theory]
    [InlineData("GET")]
    [InlineData("HEAD")]
    [InlineData("OPTIONS")]
    public async Task Locked_ReadRequest_PassesThrough(string method)
    {
        MaintenanceState state = new();
        using IDisposable lease = state.Enter("restore");
        bool nextCalled = false;
        MaintenanceModeMiddleware middleware = new(_ => { nextCalled = true; return Task.CompletedTask; }, state);

        DefaultHttpContext ctx = BuildContext(method, "/api/tournaments");
        await middleware.InvokeAsync(ctx);

        Assert.True(nextCalled, "Reads must remain available while locked.");
        Assert.NotEqual(StatusCodes.Status503ServiceUnavailable, ctx.Response.StatusCode);
    }

    [Fact]
    public async Task NotLocked_MutatingRequest_PassesThrough()
    {
        MaintenanceState state = new();
        bool nextCalled = false;
        MaintenanceModeMiddleware middleware = new(_ => { nextCalled = true; return Task.CompletedTask; }, state);

        DefaultHttpContext ctx = BuildContext("POST", "/api/tournaments");
        await middleware.InvokeAsync(ctx);

        Assert.True(nextCalled, "When unlocked, mutating requests must pass.");
    }

    [Fact]
    public async Task Locked_StatusEndpoint_IsAllowlistedEvenForMutations()
    {
        MaintenanceState state = new();
        using IDisposable lease = state.Enter("backup");
        bool nextCalled = false;
        MaintenanceModeMiddleware middleware = new(_ => { nextCalled = true; return Task.CompletedTask; }, state);

        DefaultHttpContext ctx = BuildContext("POST", "/api/backups/status");
        await middleware.InvokeAsync(ctx);

        Assert.True(nextCalled, "The status endpoint must stay reachable while locked.");
    }
}
