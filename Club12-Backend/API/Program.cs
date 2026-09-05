using API.BackgroundServices;
using API.Utils;
using API.Utils.Middlewares;

using Application.Utils.Options;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Serilog;
using Serilog.Events;

using System;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Host.AddSerilogConfig(builder.Configuration);

builder.Services
    .AddAutoMapper(cfg => { }, typeof(Program).Assembly)
    .AddDbContextConfig(builder.Configuration)
    .AddCorsConfig(builder.Configuration)
    .RegisterScoped()
    .RegisterSingletons()
    .AddCustomAuthorization()
    .AddCustomAuthentication(builder.Configuration)
    .AddCustomSwagger(builder.Configuration)
    .AddEmailConfig(builder.Configuration)
    .AddIdentityConfig(builder.Configuration)
    .AddBackupConfig(builder.Configuration)
    .AddHealthChecksConfig()
    .AddExceptionHandler<GlobalExceptionHandler>()
    .AddProblemDetails();

// Configurable roster limits; an absent section falls back to defaults.
builder.Services.Configure<RosterOptions>(
    builder.Configuration.GetSection(RosterOptions.SectionName));

builder.Services.AddControllers().AddCustomJsonOptions();

if (builder.Configuration.GetValue<bool>(ConfigurationKeys.Backup.Enabled))
{
    builder.Services.AddHostedService(sp => sp.GetRequiredService<DatabaseBackupHostedService>());
}

WebApplication app = builder.Build();

await app.ExecuteMigrationsAndSeedAsync();

app.UseSwaggerConfig(builder.Environment)
    .UseSerilogRequestLogging(options => options.GetLevel = GetRequestLoggingLevel)
    .UseCors()
    .UseMiddleware<MaintenanceModeMiddleware>()
    .UseAuthentication()
    .UseAuthorization()
    .UseMiddleware<MustChangePasswordMiddleware>()
    .UseExceptionHandlerConfig()
    .UseLoggingToRequestContextMiddleware(builder.Configuration);

app.MapControllers();
app.MapHealthCheckEndpoints();

app.LogStartupBanner();

try
{
    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, LogMessages.TerminatedUnexpectedly);
}
finally
{
    await Log.CloseAndFlushAsync();
}

// The docker-compose healthcheck polls /health/ready every 30s, so logging every successful poll at Information would drown out real request logs; failures still surface at Error regardless of path, so a flapping health check remains visible.
static LogEventLevel GetRequestLoggingLevel(HttpContext httpContext, double elapsedMs, Exception? ex)
{
    if (ex is not null || httpContext.Response.StatusCode > 499)
    {
        return LogEventLevel.Error;
    }

    if (httpContext.Request.Path.StartsWithSegments("/health"))
    {
        return LogEventLevel.Verbose;
    }

    return LogEventLevel.Information;
}

/// <summary>
/// Visibility-only shim making Program a public partial type since WebApplicationFactory needs it for integration tests.
/// </summary>
public partial class Program
{
    protected Program() { }
}
