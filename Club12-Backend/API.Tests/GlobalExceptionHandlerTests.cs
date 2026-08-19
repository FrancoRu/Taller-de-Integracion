using API.Utils;

using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

using System.Text.Json;

namespace API.Tests;

public class GlobalExceptionHandlerTests
{
    private sealed class FakeExceptionHandlerFeature : IExceptionHandlerFeature
    {
        public FakeExceptionHandlerFeature(Exception error)
        {
            Error = error;
            Path = "/api/test";
            Endpoint = null;
            RouteValues = null;
        }

        public Exception Error { get; }
        public string Path { get; }
        public Microsoft.AspNetCore.Http.Endpoint? Endpoint { get; }
        public Microsoft.AspNetCore.Routing.RouteValueDictionary? RouteValues { get; }
    }

    private static async Task<JsonDocument> InvokeAsync(Exception exception)
    {
        GlobalExceptionHandler handler = new(NullLogger<GlobalExceptionHandler>.Instance);

        ServiceCollection services = new();
        services.AddProblemDetails();
        services.AddLogging();
        using ServiceProvider serviceProvider = services.BuildServiceProvider();

        DefaultHttpContext httpContext = new()
        {
            RequestServices = serviceProvider,
        };
        httpContext.Response.Body = new MemoryStream();
        httpContext.Features.Set<IExceptionHandlerFeature>(new FakeExceptionHandlerFeature(exception));

        bool handled = await handler.TryHandleAsync(httpContext, exception, CancellationToken.None);
        Assert.True(handled);

        httpContext.Response.Body.Seek(0, SeekOrigin.Begin);
        using StreamReader reader = new(httpContext.Response.Body);
        string body = await reader.ReadToEndAsync();
        return JsonDocument.Parse(body);
    }

    [Fact]
    public async Task TryHandleAsync_UnmappedException_DoesNotExposeRawExceptionMessage()
    {
        InvalidCastException sensitiveException = new("Connection string 'Host=db;Password=Sup3rSecret' is invalid");

        using JsonDocument document = await InvokeAsync(sensitiveException);

        string detail = document.RootElement.GetProperty("detail").GetString() ?? string.Empty;

        Assert.DoesNotContain("Sup3rSecret", detail);
        Assert.DoesNotContain(sensitiveException.Message, detail);
        Assert.Equal(StatusCodes.Status500InternalServerError, document.RootElement.GetProperty("status").GetInt32());
    }

    [Fact]
    public async Task TryHandleAsync_ArgumentNullException_StillExposesValidationMessage()
    {
#pragma warning disable S3928
        ArgumentNullException validationException = new("teamId", "Team id is required.");
#pragma warning restore S3928

        using JsonDocument document = await InvokeAsync(validationException);

        string detail = document.RootElement.GetProperty("detail").GetString() ?? string.Empty;

        Assert.Contains(validationException.Message, detail);
        Assert.Equal(StatusCodes.Status400BadRequest, document.RootElement.GetProperty("status").GetInt32());
    }
}
