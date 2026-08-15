using API.Utils.Converters;
using API.Utils.Middlewares;
using Application.Interfaces.Mappers;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Application.Services;
using Application.Utils.Helper.Email;
using Application.Utils.Helper.SupabaseHelper;
using Application.Utils.Mappers;
using Domain.Enums;
using FluentEmail.MailKitSmtp;
using Infrastructure.Identity;
using Infrastructure.Persistance;
using Infrastructure.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace API.Utils;

/// <summary>
/// Provides extension methods for configuring application startup, including logging, database context, CORS, authentication, authorization, Swagger, JSON options, and dependency injection.
/// </summary>
public static class StartupExtensions
{
    /// <summary>
    /// Configures Serilog logging for the application host.
    /// </summary>
    public static void AddSerilogConfig(this IHostBuilder hostBuilder, IConfiguration configuration)
    {
        hostBuilder.UseSerilog((context, config) => config.ReadFrom.Configuration(configuration));
    }

    /// <summary>
    /// Adds and configures the application's database context and dependency injection for the database.
    /// </summary>
    public static IServiceCollection AddDbContextConfig(this IServiceCollection services, IConfiguration configuration)
    {
        string? connectionString = configuration.GetConnectionString("DbConnection");
        if (connectionString is null)
        {
            Log.Fatal("Connection string is missing. Using default or fallback connection string.");
            throw new ArgumentException("The connection string should be initialized already.");
        }
        services.AddDbContext<ApplicationDBContext>(options => options.UseNpgsql(connectionString));
        services.AddScoped<IClub12DBContext, ApplicationDBContext>();

        return services;
    }

    /// <summary>
    /// Adds and configures CORS policies for the application.
    /// </summary>
    public static IServiceCollection AddCorsConfig(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                policy.WithOrigins(configuration.GetSection("AllowedOrigins").Get<string[]>()!)
                      .AllowAnyHeader()
                      .AllowAnyMethod();
            });
        });

        return services;
    }

    /// <summary>
    /// Runs EF migrations for both <see cref="ApplicationDBContext"/> and <see cref="IdentityAppDbContext"/>,
    /// then seeds the initial admin user.
    /// </summary>
    public static async Task ExecuteMigrationsAndSeedAsync(this WebApplication app)
    {
        await using AsyncServiceScope scope = app.Services.CreateAsyncScope();

        // Main domain DB
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        await db.Database.MigrateAsync();

        // Identity DB
        IdentityAppDbContext identityDb = scope.ServiceProvider.GetRequiredService<IdentityAppDbContext>();
        await identityDb.Database.MigrateAsync();

        // Seed admin user
        IdentitySeeder seeder = scope.ServiceProvider.GetRequiredService<IdentitySeeder>();
        await seeder.SeedAsync();
    }

    /// <summary>
    /// Configures Swagger middleware for API documentation in non-production environments.
    /// </summary>
    public static void UseSwaggerConfig(this WebApplication app, IHostEnvironment env)
    {
        if (!env.IsProduction())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }
    }

    /// <summary>
    /// Configures exception handling and status code pages middleware.
    /// </summary>
    public static void UseExceptionHandlerConfig(this WebApplication app)
    {
        app.UseStatusCodePages();
        app.UseExceptionHandler();
    }

    // One policy per domain role — single source of truth via UserRoleType enum.
    private static readonly string[] _roleNames =
    [
        UserRoleType.ADMIN.ToRoleName(),
        UserRoleType.OWNER.ToRoleName(),
        UserRoleType.TOURNAMENT_MANAGER.ToRoleName(),
        UserRoleType.TEAM_MANAGER.ToRoleName(),
        UserRoleType.GUEST.ToRoleName(),
    ];

    /// <summary>
    /// Adds custom authorization policies based on <see cref="UserRoleType"/> domain roles.
    /// </summary>
    public static IServiceCollection AddCustomAuthorization(this IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            foreach (string role in _roleNames)
                options.AddPolicy(role, policy => policy.RequireRole(role));
        });

        return services;
    }

    /// <summary>
    /// Adds and configures JWT authentication for the application.
    /// </summary>
    public static IServiceCollection AddCustomAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        string? jwtSecret = configuration.GetSection("JWT:Key")?.Value;
        if (string.IsNullOrEmpty(jwtSecret))
            throw new ArgumentException("The JWT is missing or empty in configuration.");

        services.AddAuthentication(options =>
        {
            options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
        }).AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer           = true,
                ValidateAudience         = true,
                ValidateLifetime         = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer              = configuration["JWT:Issuer"],
                ValidAudience            = configuration["JWT:Audience"],
                IssuerSigningKey         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret))
            };
        });

        return services;
    }

    /// <summary>
    /// Adds middleware for logging requests to the request context if enabled in configuration.
    /// </summary>
    public static WebApplication UseLoggingToRequestContextMiddleware(this WebApplication app, IConfiguration configuration)
    {
        bool useLoggingMiddleware = configuration.GetValue<bool>("UseLoggingMiddleware", false);
        if (useLoggingMiddleware)
        {
            app.UseMiddleware<RequestLoggingMiddleware>();
            Log.Information("Request logging middleware enabled.");
        }
        else
        {
            Log.Information("Request logging middleware disabled.");
        }
        return app;
    }

    /// <summary>
    /// Adds custom JSON serialization options for MVC, including enum and date converters.
    /// </summary>
    public static void AddCustomJsonOptions(this IMvcBuilder builder)
    {
        builder.AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
            options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
            options.JsonSerializerOptions.Converters.Add(new DateOnlyJsonConverter());
        });
    }

    /// <summary>
    /// Adds and configures Swagger for API documentation, including security definitions and schema filters.
    /// </summary>
    public static IServiceCollection AddCustomSwagger(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSwaggerGen(context =>
        {
            context.SwaggerDoc("v1", new OpenApiInfo
            {
                Title   = configuration["Swagger:Title"],
                Version = configuration["Swagger:Version"],
            });

            string xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            context.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFilename));
            context.ResolveConflictingActions(apiDescriptions => apiDescriptions.First());

            context.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token in the text input below.",
                Name        = "Authorization",
                In          = ParameterLocation.Header,
                Type        = SecuritySchemeType.ApiKey,
                Scheme      = "Bearer"
            });

            context.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" },
                        Scheme    = "oauth2",
                        Name      = "Bearer",
                        In        = ParameterLocation.Header
                    },
                    new List<string>()
                }
            });

            context.SchemaFilter<DisplayEnumSchemaFilter>();
        });

        return services;
    }

    /// <summary>
    /// Registers singleton services for dependency injection.
    /// </summary>
    public static IServiceCollection RegisterSingletons(this IServiceCollection services)
    {
        services.AddSingleton<SupabaseHelper>();
        return services;
    }

    /// <summary>
    /// Registers Identity DbContext, ASP.NET Core Identity services, <see cref="IAuthenticationService"/>,
    /// <see cref="IUserManagementService"/>, and <see cref="IdentitySeeder"/>.
    /// </summary>
    public static IServiceCollection AddIdentityConfig(
        this IServiceCollection services, IConfiguration configuration)
    {
        string? connectionString = configuration.GetConnectionString("DbConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentException("The connection string should be initialized already.");

        services.AddDbContext<IdentityAppDbContext>(options => options.UseNpgsql(connectionString));

        services.AddIdentityCore<ApplicationUser>(options =>
        {
            options.User.RequireUniqueEmail = true;
        })
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<IdentityAppDbContext>()
            .AddSignInManager()
            .AddDefaultTokenProviders();

        services.AddScoped<IAuthenticationService,  IdentityAuthenticationService>();
        services.AddScoped<IUserManagementService,  IdentityUserManagementService>();
        services.AddScoped<IdentitySeeder>();

        return services;
    }

    /// <summary>
    /// Registers scoped services and repositories for dependency injection using reflection.
    /// </summary>
    public static IServiceCollection RegisterScoped(this IServiceCollection services)
    {
        string? serviceInterfaceNamespace   = typeof(IDivisionService).Namespace;
        string? serviceImplNamespace        = typeof(DivisionService).Namespace;
        string? repositoryInterfaceNamespace = typeof(IBlogPostRepository).Namespace;
        string? repositoryImplNamespace     = typeof(BlogPostRepository).Namespace;
        string? mapperInterfaceNamespace = typeof(IScorerMapper).Namespace;
        string? mapperImplNamespace = typeof(ScorerMapper).Namespace;
        string  serviceSuffix               = "Service";
        string  repositorySuffix            = "Repository";
        string  mapperSuffix = "Mapper";

        ArgumentNullException.ThrowIfNull(serviceInterfaceNamespace,    "Service interface namespace cannot be null.");
        ArgumentNullException.ThrowIfNull(serviceImplNamespace,         "Service implementation namespace cannot be null.");
        ArgumentNullException.ThrowIfNull(repositoryInterfaceNamespace, "Repository interface namespace cannot be null.");
        ArgumentNullException.ThrowIfNull(repositoryImplNamespace,      "Repository implementation namespace cannot be null.");
        ArgumentNullException.ThrowIfNull(mapperInterfaceNamespace, "Mapper interface namespace cannot be null.");
        ArgumentNullException.ThrowIfNull(mapperImplNamespace,      "Mapper implementation namespace cannot be null.");

        Assembly serviceAssembly  = typeof(AuthService).Assembly;
        Assembly IServiceAssembly = typeof(IAuthService).Assembly;
        Assembly repoAssembly     = typeof(BlogPostRepository).Assembly;
        Assembly IRepoAssembly    = typeof(IBlogPostRepository).Assembly;
        Assembly mapperAssembly   = typeof(ScorerMapper).Assembly;
        Assembly IMapperAssembly  = typeof(IScorerMapper).Assembly;

        services.HelperRegisterScoped(serviceAssembly, IServiceAssembly, serviceSuffix, serviceInterfaceNamespace, serviceImplNamespace);
        services.HelperRegisterScoped(repoAssembly, IRepoAssembly, repositorySuffix, repositoryInterfaceNamespace, repositoryImplNamespace);
        services.HelperRegisterScoped(mapperAssembly, IMapperAssembly, mapperSuffix, mapperInterfaceNamespace, mapperImplNamespace);


        services.AddScoped<IUnitOfWork, UnitOfWork>();
        return services;
    }

    private static void HelperRegisterScoped(this IServiceCollection services, Assembly implementationAssembly, Assembly interfaceAssembly, string suffix, string iNamespace, string implNamespace)
    {
        string interfacePrefix = "I";

        IEnumerable<Type> interfaces = interfaceAssembly.GetTypes()
            .Where(t => t.IsInterface && t.Namespace == iNamespace
                     && t.Name.StartsWith(interfacePrefix) && t.Name.EndsWith(suffix));

        foreach (Type iface in interfaces)
        {
            Type? implementation = implementationAssembly.GetTypes()
                .FirstOrDefault(t => t.IsClass && !t.IsAbstract
                                  && t.Namespace == implNamespace
                                  && t.Name == iface.Name[1..]);

            if (implementation is not null)
                services.AddScoped(iface, implementation);
        }
    }
    /// <summary>
    /// Registers FluentEmail with Mailgun and binds <see cref="IEmailService"/>
    /// to <see cref="FluentEmailHelper"/>.
    /// </summary>
    public static IServiceCollection AddEmailConfig(
       this IServiceCollection services, IConfiguration configuration)
    {
        string smtpHost = configuration["Smtp:Host"]
            ?? throw new ArgumentException("Smtp:Host is missing from configuration.");
        int smtpPort = int.Parse(configuration["Smtp:Port"]
            ?? throw new ArgumentException("Smtp:Port is missing from configuration."));
        string username = configuration["Smtp:Username"]
            ?? throw new ArgumentException("Smtp:Username is missing from configuration.");
        string password = configuration["Smtp:Password"]
            ?? throw new ArgumentException("Smtp:Password is missing from configuration.");
        bool useSsl = bool.Parse(configuration["Smtp:UseSsl"] ?? "true");
        string fromEmail = configuration["Smtp:FromEmail"] ?? "noreply@club12.com";
        string fromName = configuration["Smtp:FromName"] ?? "Club12";

        services
            .AddFluentEmail(fromEmail, fromName)
            .AddMailKitSender(new SmtpClientOptions
            {
                Server = smtpHost,
                Port = smtpPort,
                UseSsl = true,
                User = username,
                Password = password,
                RequiresAuthentication = true
            });

        services.AddScoped<IEmailService, FluentEmailHelper>();

        return services;
    }
}
