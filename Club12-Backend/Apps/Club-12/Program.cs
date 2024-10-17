using Club12.Entities;
using Club12.Utils;
using Microsoft.EntityFrameworkCore;
using Persistence;
using Serilog;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Add Serilog logging  
builder.Host.UseSerilog((context, configuration) =>
    configuration.ReadFrom.Configuration(context.Configuration));

builder.Services.AddAutoMapper(typeof(Program));
builder.Services.AddScoped<IClub12DBContext, ApplicationDBContext>();

string? connectionString = builder.Configuration.GetConnectionString("DbConnection");
string? jwtSecret = builder.Configuration.GetSection("JWT:Key").Value;

if (jwtSecret is null)
{
    Log.Fatal("There wasn't a JWT Key in the appsettings.");
    throw new ArgumentException("The JWT Key should be initialized already.");
}

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
builder.Services.AddCustomAuthorization();
builder.Services.AddCustomAuthentication(builder.Configuration);
builder.Services.AddControllers().AddCustomJsonOptions();
builder.Services.AddCustomSwagger(builder.Configuration);

WebApplication app = builder.Build();

using (IServiceScope scope = app.Services.CreateScope())
{
    ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
    db.Database.Migrate();

    await app.Services.EnsureAdminUserExists();
}

app.UseSerilogRequestLogging();

if (!builder.Environment.IsProduction())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

Log.Information("----- Starting up -----");
Log.Information("\r\n                                                                \r\n  ####    ##       ##  ##   #####               ##      ####   \r\n ##  ##   ##       ##  ##   ##  ##             ###     ##  ##  \r\n ##       ##       ##  ##   #####               ##        ##   \r\n ##       ##       ##  ##   ##  ##              ##       ##    \r\n ##  ##   ##       ##  ##   ##  ##              ##      ##     \r\n  ####    ######   ######   #####             ######   ######  \r\n                                                               \r\n");
Log.Information("----- Started     -----");
Log.CloseAndFlush();

app.Run();
