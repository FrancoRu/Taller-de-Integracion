using Club12.Entities.DivisionEntity;
using Club12.Entities.PlayerEntity;
using Club12.Entities.TeamEntity;
using Club12.Entities.UserEntity;
using Club12.Services.Auth;
using Club12.Services.Auth.Implementation;
using Club12.Services.DataAccessLayer.GenericEntity;
using Club12.Services.DataAccessLayer.GenericEntity.Implementation;
using Club12.Services.Divisions;
using Club12.Services.Divisions.Implementation;
using Club12.Services.Players;
using Club12.Services.Players.Implementation;
using Club12.Services.Teams;
using Club12.Services.Teams.Implementation;
using Club12.Services.Users;
using Club12.Services.Users.Implementation;
using Club12.Services.Utils;
using Club12.Services.Utils.Cloudfare;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Persistence;
using System.Reflection;
using System.Text;
using System.Text.Json.Serialization;

namespace Club12.Utils;

/// <summary>
/// Extension methods for registering application services in the dependency injection container.
/// </summary>
public static class ServiceExtension
{
    private static readonly Dictionary<string, string> _roles = new()
    {
            { "SuperAdmin", "SuperAdmin" }
    };

    /// <summary>
    /// Registers application services in the dependency injection container.
    /// </summary>
    /// <param name="collection">The <see cref="IServiceCollection"/> to add services to.</param>
    public static void RegisterApplicationServices(this IServiceCollection collection)
    {
        collection.AddScoped<IDivisionService, DivisionService>();
        collection.AddScoped<DivisionService>();
        collection.AddScoped<IGenericService<Division>, GenericService<Division>>();
        collection.AddScoped<ITeamService, TeamService>();
        collection.AddScoped<TeamService>();
        collection.AddScoped<IGenericService<Team>, GenericService<Team>>();
        collection.AddScoped<IPlayerService, PlayerService>();
        collection.AddScoped<PlayerService>();
        collection.AddScoped<IGenericService<Player>, GenericService<Player>>();
        collection.AddScoped<IUserService, UserService>();
        collection.AddScoped<UserService>();
        collection.AddScoped<IGenericService<User>, GenericService<User>>();
        collection.AddScoped<IAuthService, AuthService>();
        collection.AddScoped<AuthService>();
        collection.AddScoped<ICloudflareService, CloudflareService>();
        collection.AddScoped<CloudflareService>();
    }

    /// <summary>
    /// Adds custom authorization policies based on predefined roles.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
    public static void AddCustomAuthorization(this IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            _roles.ToList().ForEach(role =>
            {
                options.AddPolicy(role.Key, policy =>
                {
                    policy.RequireRole(role.Value);
                });
            });
        });
    }

    /// <summary>
    /// Adds custom authentication with JWT bearer token.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
    public static void AddCustomAuthentication(this IServiceCollection services)
    {
        IConfiguration configuration = services.BuildServiceProvider().GetRequiredService<IConfiguration>();
        string? jwtSecret = configuration.GetSection("JWT:Key")?.Value;

        if (string.IsNullOrEmpty(jwtSecret))
        {
            throw new ArgumentException("The JWT is missing or empty in configuration.");
        }

        services.AddAuthentication(options =>
        {
            options.DefaultScheme = "Bearer";
        }).AddJwtBearer("Bearer", options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = configuration["JWT:Issuer"],
                ValidAudience = configuration["JWT:Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret))
            };
        });
    }

    /// <summary>
    /// Adds custom JSON serialization options.
    /// </summary>
    /// <param name="builder">The <see cref="IMvcBuilder"/> to configure JSON options.</param>
    public static void AddCustomJsonOptions(this IMvcBuilder builder)
    {
        builder.AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        });
    }

    /// <summary>
    /// Adds custom Swagger configuration.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
    public static void AddCustomSwagger(this IServiceCollection services)
    {
        IConfiguration configuration = services.BuildServiceProvider().GetRequiredService<IConfiguration>();
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = configuration["Swagger:Title"],
                Version = configuration["Swagger:Version"],
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
    }

    /// <summary>
    /// Ensures that an admin user exists in the database. If no admin user is found, it creates one with default credentials.
    /// </summary>
    /// <param name="serviceProvider">The <see cref="IServiceProvider"/> instance.</param>
    public static void EnsureAdminUserExists(this IServiceProvider serviceProvider)
    {
        using IServiceScope scope = serviceProvider.CreateScope();
        ApplicationDBContext dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IUserService userService = scope.ServiceProvider.GetRequiredService<IUserService>();

        User? adminUser = userService.GetUserByUserName("admin");

        if (adminUser is null)
        {
            User adminEntity = new()
            {
                Username = "admin",
                Password = Encrypt.Hash("root"),
                Role = "SuperAdmin",
                DateCreated = DateTime.UtcNow,
                DateUpdated = DateTime.UtcNow,
            };

            dbContext.Users.Add(adminEntity);
            dbContext.SaveChanges();
        }
    }
}
