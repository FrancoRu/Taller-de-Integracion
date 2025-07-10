using Entities.Models.BlogPosts;
using Entities.Models.Divisions;
using Entities.Models.Matches;
using Entities.Models.Players;
using Entities.Models.PlayerSanctions;
using Entities.Models.PlayerStatistics;
using Entities.Models.PlayoffSeries;
using Entities.Models.Staffs;
using Entities.Models.Teams;
using Entities.Models.Tournaments;
using Entities.Models.Users;
using Entities.Models.Venues;

using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

using Persistance;

using Services.Auth;
using Services.Auth.Implementation;
using Services.BackgroundServices;
using Services.DataAccessLayer.GenericEntity;
using Services.DataAccessLayer.GenericEntity.Implementation;
using Services.Services.BlogPosts;
using Services.Services.BlogPosts.Implementation;
using Services.Services.Divisions;
using Services.Services.Divisions.Implementation;
using Services.Services.Matches;
using Services.Services.Matches.Implementation;
using Services.Services.Players;
using Services.Services.Players.Implementation;
using Services.Services.PlayerSanctions;
using Services.Services.PlayerSanctions.Implementation;
using Services.Services.PlayerStatistics;
using Services.Services.PlayerStatistics.Implementation;
using Services.Services.PlayoffSeries;
using Services.Services.PlayoffSeries.Implementation;
using Services.Services.Staffs;
using Services.Services.Staffs.Implementation;
using Services.Services.Teams;
using Services.Services.Teams.Implementation;
using Services.Services.Tournaments;
using Services.Services.Tournaments.Implementation;
using Services.Services.Users;
using Services.Services.Users.Implementation;
using Services.Services.Venues;
using Services.Services.Venues.Implementation;
using Services.Utils;
using Services.Utils.Cloudfare;
using Services.Utils.Cloudfare.Implementation;
using Services.Utils.Excel;
using Services.Utils.Excel.Implementation;

using System.Reflection;
using System.Text;
using System.Text.Json.Serialization;


namespace Club12.API.Utils;

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
        collection.AddScoped<IAuthService, AuthService>();
        collection.AddScoped<IDivisionService, DivisionService>();
        collection.AddScoped<IGenericService<Division>, GenericService<Division>>();
        collection.AddScoped<IMatchService, MatchService>();
        collection.AddScoped<IGenericService<Match>, GenericService<Match>>();
        collection.AddScoped<IPlayerService, PlayerService>();
        collection.AddScoped<IGenericService<Player>, GenericService<Player>>();
        collection.AddScoped<IPlayerSanctionService, PlayerSanctionService>();
        collection.AddScoped<IGenericService<PlayerSanction>, GenericService<PlayerSanction>>();
        collection.AddScoped<IPlayerStatisticService, PlayerStatisticService>();
        collection.AddScoped<IGenericService<PlayerStatistic>, GenericService<PlayerStatistic>>();
        collection.AddScoped<IGenericService<Team>, GenericService<Team>>();
        collection.AddScoped<ITeamService, TeamService>();
        collection.AddScoped<IGenericService<Tournament>, GenericService<Tournament>>();
        collection.AddScoped<ITournamentService, TournamentService>();
        collection.AddScoped<IGenericService<User>, GenericService<User>>();
        collection.AddScoped<IUserService, UserService>();
        collection.AddScoped<IGenericService<Venue>, GenericService<Venue>>();
        collection.AddScoped<IVenueService, VenueService>();
        collection.AddScoped<ICloudflareService, CloudflareService>();
        collection.AddScoped<IExcelService, ExcelService>();
        collection.AddScoped<IBlogPostService, BlogPostService>();
        collection.AddScoped<IGenericService<BlogPost>, GenericService<BlogPost>>();
        collection.AddScoped<IStaffService, StaffService>();
        collection.AddScoped<IGenericService<Staff>, GenericService<Staff>>();
        collection.AddScoped<IPlayoffSeriesService, PlayoffSeriesService>();
        collection.AddScoped<IGenericService<PlayoffSerie>, GenericService<PlayoffSerie>>();
        collection.AddHostedService<SanctionCleanupService>();
    }

    /// <summary>
    /// Adds custom authorization policies based on predefined roles.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
    public static void AddCustomAuthorization(this IServiceCollection services) => services.AddAuthorization(options =>
                                                                                        {
                                                                                            _roles.ToList().ForEach(role =>
                                                                                            {
                                                                                                options.AddPolicy(role.Key, policy =>
                                                                                                {
                                                                                                    policy.RequireRole(role.Value);
                                                                                                });
                                                                                            });
                                                                                        });

    /// <summary>
    /// Adds custom authentication with JWT bearer token.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
    /// <param name="configuration">The <see cref="IConfiguration"/> to access configuration settings.</param>
    public static void AddCustomAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
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
    public static void AddCustomJsonOptions(this IMvcBuilder builder) => builder.AddJsonOptions(options =>
                                                                              {
                                                                                  options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
                                                                              });

    /// <summary>
    /// Adds custom Swagger configuration.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
    /// <param name="configuration">The <see cref="IConfiguration"/> to access configuration settings.</param>
    public static void AddCustomSwagger(this IServiceCollection services, IConfiguration configuration) => services.AddSwaggerGen(context =>
                                                                                                                {
                                                                                                                    context.SwaggerDoc("v1", new OpenApiInfo
                                                                                                                    {
                                                                                                                        Title = configuration["Swagger:Title"],
                                                                                                                        Version = configuration["Swagger:Version"],
                                                                                                                    });

                                                                                                                    string xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                                                                                                                    context.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFilename));

                                                                                                                    context.ResolveConflictingActions(apiDescriptions => apiDescriptions.First());

                                                                                                                    context.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                                                                                                                    {
                                                                                                                        Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token in the text input below.",
                                                                                                                        Name = "Authorization",
                                                                                                                        In = ParameterLocation.Header,
                                                                                                                        Type = SecuritySchemeType.ApiKey,
                                                                                                                        Scheme = "Bearer"
                                                                                                                    });


                                                                                                                    context.AddSecurityRequirement(new OpenApiSecurityRequirement
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

                                                                                                                    context.SchemaFilter<DisplayEnumSchemaFilter>();
                                                                                                                });

    /// <summary>
    /// Ensures that an admin user exists in the database. If no admin user is found, it creates one with default credentials.
    /// </summary>
    /// <param name="serviceProvider">The <see cref="IServiceProvider"/> instance.</param>
    public static async Task EnsureAdminUserExists(this IServiceProvider serviceProvider)
    {
        using IServiceScope scope = serviceProvider.CreateScope();
        ApplicationDBContext dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IUserService userService = scope.ServiceProvider.GetRequiredService<IUserService>();

        User? adminUser = await userService.GetUserByUserNameAsync("admin");

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
