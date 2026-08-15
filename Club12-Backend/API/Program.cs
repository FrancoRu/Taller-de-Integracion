using API.BackgroundServices;
using API.Utils;
using API.Utils.Middlewares;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using System;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Serilog
builder.Host.AddSerilogConfig(builder.Configuration);

builder.Services
    .AddAutoMapper(cfg => { }, typeof(Program).Assembly)
    .AddDbContextConfig(builder.Configuration)
    .AddCorsConfig(builder.Configuration)
    .RegisterScoped()
    .RegisterSingletons()
    .AddCustomAuthorization()
    .AddCustomAuthentication(builder.Configuration)
    .AddCustomSwagger(builder.Configuration);

builder.Services.AddControllers().AddCustomJsonOptions();
builder.Services.AddEmailConfig(builder.Configuration);
builder.Services.AddIdentityConfig(builder.Configuration);

// Scheduled database backups (opt-in via Backup:Enabled, default false)
builder.Services.AddBackupConfig(builder.Configuration);
if (builder.Configuration.GetValue<bool>("Backup:Enabled"))
{
    builder.Services.AddHostedService(sp => sp.GetRequiredService<DatabaseBackupHostedService>());
}

// Exception Handler & Problem Details
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

WebApplication app = builder.Build();

// Migrations (ApplicationDB + IdentityDB) + admin user seeding
await app.ExecuteMigrationsAndSeedAsync();

// Swagger
app.UseSwaggerConfig(builder.Environment);

// Logging, CORS, Auth, Controllers, Exception Handling
app.UseSerilogRequestLogging()
    .UseCors()
    .UseAuthentication()
    .UseAuthorization()
    .UseMiddleware<MustChangePasswordMiddleware>();

app.MapControllers();
app.UseExceptionHandlerConfig();
app.UseLoggingToRequestContextMiddleware(builder.Configuration);

// Startup logs
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

// Visibility-only shim: WebApplicationFactory<Program> (used by integration tests)
// requires the top-level Program class to be a public partial type. This adds no
// runtime behavior and does not alter any code path.
public partial class Program { }