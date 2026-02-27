using API.Utils;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using System;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Serilog
builder.Host.AddSerilogConfig(builder.Configuration);

builder.Services
    .AddAutoMapper(typeof(Program))
    .AddDbContextConfig(builder.Configuration)
    .AddCorsConfig(builder.Configuration)
    .RegisterScoped()
    .RegisterSingletons()
    .AddCustomAuthorization()
    .AddCustomAuthentication(builder.Configuration)
    .AddCustomSwagger(builder.Configuration);


builder.Services.AddControllers().AddCustomJsonOptions();

// Exception Handler & Problem Details
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

WebApplication app = builder.Build();

// Database migration & admin user creation
app.ExecuteMigrations();

// Swagger
app.UseSwaggerConfig(builder.Environment);

// Logging, CORS, Auth, Controllers, Exception Handling
app.UseSerilogRequestLogging()
    .UseCors()
    .UseAuthentication()
    .UseAuthorization();

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
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}