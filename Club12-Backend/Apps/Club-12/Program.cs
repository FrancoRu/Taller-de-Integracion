using Club12.Entities;
using Club12.Utils;
using Microsoft.EntityFrameworkCore;
using Persistence;
using Serilog;
using System.Reflection;
using System.Text.Json.Serialization;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Add serilog logging  
builder.Host.UseSerilog((context, configuration) =>
    configuration.ReadFrom.Configuration(context.Configuration));

builder.Services.AddScoped<IClub12DBContext, ApplicationDBContext>();

string? connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

if (connectionString is null)
{
    Log.Fatal("Connection string is missing. Using default or fallback connection string.");
    throw new ArgumentException("The connection string should be initialized already.");
}

builder.Services.AddDbContext<ApplicationDBContext>(options => options.UseNpgsql(connectionString));

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(builder.Configuration.GetSection("AllowedOrigins").Get<string[]>()!)
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

builder.Services.RegisterApplicationServices();

builder.Services.AddControllers().AddJsonOptions(x =>
    x.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = builder.Configuration["Swagger:Title"],
        Version = builder.Configuration["Swagger:Version"],
    });
    string xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    c.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFilename));
    c.ResolveConflictingActions(apiDescriptions => apiDescriptions.First());
});

WebApplication app = builder.Build();

if (!builder.Environment.IsProduction())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseSerilogRequestLogging();

app.UseCors();
app.UseAuthorization();
app.MapControllers();

Log.Information("----- Starting up -----");
Log.Information("                                                               \r\n  ####    ##       ##  ##   #####               ##      ####   \r\n ##  ##   ##       ##  ##   ##  ##             ###     ##  ##  \r\n ##       ##       ##  ##   #####               ##        ##   \r\n ##       ##       ##  ##   ##  ##              ##       ##    \r\n ##  ##   ##       ##  ##   ##  ##              ##      ##     \r\n  ####    ######   ######   #####             ######   ######  \r\n                                                               \r\n");
Log.Information("----- Started     -----");
Log.CloseAndFlush();

app.Run();

/// <summary>
/// This class is part of the program and is used for tests.
/// More information can be found at: https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests?view=aspnetcore-8.0
/// </summary>
#pragma warning disable S1118
public partial class Program { }
#pragma warning restore S1118