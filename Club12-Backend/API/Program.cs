using API.BackgroundServices;
using API.Utils;
using API.Utils.Middlewares;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
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
    .AddExceptionHandler<GlobalExceptionHandler>()
    .AddProblemDetails();

builder.Services.AddControllers().AddCustomJsonOptions();

if (builder.Configuration.GetValue<bool>(ConfigurationKeys.Backup.Enabled))
{
    builder.Services.AddHostedService(sp => sp.GetRequiredService<DatabaseBackupHostedService>());
}

WebApplication app = builder.Build();

await app.ExecuteMigrationsAndSeedAsync();

app.UseSwaggerConfig(builder.Environment)
    .UseSerilogRequestLogging()
    .UseCors()
    .UseAuthentication()
    .UseAuthorization()
    .UseMiddleware<MustChangePasswordMiddleware>()
    .UseExceptionHandlerConfig()
    .UseLoggingToRequestContextMiddleware(builder.Configuration);

app.MapControllers();

Log.Information("----- Starting up -----");
Log.Information(@"

  ####    ##       ##  ##   #####               ##      ####
 ##  ##   ##       ##  ##   ##  ##             ###     ##  ##
 ##       ##       ##  ##   #####               ##        ##
 ##       ##       ##  ##   ##  ##              ##       ##
 ##  ##   ##       ##  ##   ##  ##              ##      ##
  ####    ######   ######   #####             ######   ######

");
Log.Information("----- Started     -----");

try
{
    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    await Log.CloseAndFlushAsync();
}

/// <summary>
/// Visibility-only shim: WebApplicationFactory&lt;Program&gt; (used by integration
/// tests) requires the top-level Program class to be a public partial type.
/// This adds no runtime behavior and does not alter any code path.
/// </summary>
public partial class Program { }
