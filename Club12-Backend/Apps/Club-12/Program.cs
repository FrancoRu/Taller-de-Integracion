using Club12.Entities;
using Club12.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Persistence;
using Serilog;
using System.Reflection;
using System.Text;
using System.Text.Json.Serialization;
using DotNetEnv;

Env.Load();

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Add serilog logging  
builder.Host.UseSerilog((context, configuration) =>
    configuration.ReadFrom.Configuration(context.Configuration));

builder.Services.AddAutoMapper(typeof(Program));
builder.Services.AddScoped<IClub12DBContext, ApplicationDBContext>();

try
{
    string? connectionString = builder.Configuration.GetConnectionString("DbConnection");

    if (connectionString is null)
    {
        Log.Fatal("Connection string is missing. Using default or fallback connection string.");
        throw new ArgumentException("The connection string should be initialized already.");
    }
    builder.Services.AddDbContext<ApplicationDBContext>(options => options.UseNpgsql(connectionString));
}
catch (ArgumentException e) {
    Log.Fatal(e.Message);
}



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

try
{
    string jwtSecret = Environment.GetEnvironmentVariable("JWT_TOKEN_KEY") ?? throw new InvalidOperationException("La variable de entorno JWT_TOKEN_KEY no está configurada.");
    builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = "Bearer";
    }).AddJwtBearer("Bearer", options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(jwtSecret)),
            ValidateIssuer = false,
            ValidateAudience = false,
            ClockSkew = TimeSpan.Zero
        };
    });

}
catch (InvalidOperationException e) {
    Log.Fatal(e.Message);
}

builder.Services.AddControllers().AddJsonOptions(x =>
    x.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles);

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = builder.Configuration["Swagger:Title"],
        Version = builder.Configuration["Swagger:Version"],
    });
    string xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    c.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFilename));
    c.ResolveConflictingActions(apiDescriptions => apiDescriptions.First());

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token in the text input below.",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                },
                Scheme = "oauth2",
                Name = "Bearer",
                In = ParameterLocation.Header
            },
            new List<string>()
        }
    });
});

WebApplication app = builder.Build();

using (IServiceScope scope = app.Services.CreateScope())
{
    ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
    db.Database.Migrate();

    app.Services.EnsureAdminUserExists();
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
Log.Information("                                                               \r\n  ####    ##       ##  ##   #####               ##      ####   \r\n ##  ##   ##       ##  ##   ##  ##             ###     ##  ##  \r\n ##       ##       ##  ##   #####               ##        ##   \r\n ##       ##       ##  ##   ##  ##              ##       ##    \r\n ##  ##   ##       ##  ##   ##  ##              ##      ##     \r\n  ####    ######   ######   #####             ######   ######  \r\n                                                               \r\n");
Log.Information("----- Started     -----");
Log.CloseAndFlush();

app.Run();