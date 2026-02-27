using API.Utils.Converters;
using API.Utils.Middlewares;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Application.Services;
using Application.Utils.Helper.SupabaseHelper;
using Domain;
using Infrastructure;
using Infrastructure.Repositories;
using Microsoft.AspNetCore.Builder;
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
    /// Executes database migrations at application startup.
    /// </summary>
    public static void ExecuteMigrations(this WebApplication app)
    {
        using IServiceScope scope = app.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        db.Database.Migrate();
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

    // Autorización
    private static readonly Dictionary<string, string> _roles = new()
    {
        { "SuperAdmin", "SuperAdmin" }
    };

    /// <summary>
    /// Adds custom authorization policies based on predefined roles.
    /// </summary>
    public static IServiceCollection AddCustomAuthorization(this IServiceCollection services)
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

        return services;
    }

    // Autenticación
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

        return services;
    }

    // Middleware de logging
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

    // JSON options
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

    // Swagger
    /// <summary>
    /// Adds and configures Swagger for API documentation, including security definitions and schema filters.
    /// </summary>
    public static IServiceCollection AddCustomSwagger(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSwaggerGen(context =>
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
    /// Registers scoped services and repositories for dependency injection using reflection.
    /// </summary>
    public static IServiceCollection RegisterScoped(this IServiceCollection services)
    {
        string? serviceInterfaceNamespace = typeof(IDivisionService).Namespace;
        string? serviceImplNamespace = typeof(DivisionService).Namespace;
        string? repositoryInterfaceNamespace = typeof(IBlogPostRepository).Namespace;
        string? repositoryImplNamespace = typeof(BlogPostRepository).Namespace;
        string serviceSuffix = "Service";
        string repositorySuffix = "Repository";
        string interfacePrefix = "I";

        ArgumentNullException.ThrowIfNull(serviceInterfaceNamespace, "Service interface namespace cannot be null.");
        ArgumentNullException.ThrowIfNull(serviceImplNamespace, "Service implementation namespace cannot be null.");
        ArgumentNullException.ThrowIfNull(repositoryInterfaceNamespace, "Repository interface namespace cannot be null.");
        ArgumentNullException.ThrowIfNull(repositoryImplNamespace, "Repository implementation namespace cannot be null.");

        Assembly serviceAssembly = typeof(AuthService).Assembly;
        Assembly IServiceAssembly = typeof(IAuthService).Assembly;
        Assembly repoAssembly = typeof(BlogPostRepository).Assembly;
        Assembly IRepoAssembly = typeof(IBlogPostRepository).Assembly;

        IEnumerable<Type> serviceInterfaces = IServiceAssembly.GetTypes()
            .Where(t => t.IsInterface && t.Namespace == serviceInterfaceNamespace && t.Name.StartsWith(interfacePrefix) && t.Name.EndsWith(serviceSuffix));

        foreach (Type iface in serviceInterfaces)
        {
            Type? implementation = serviceAssembly.GetTypes()
                .FirstOrDefault(t => t.IsClass && !t.IsAbstract && t.Namespace == serviceImplNamespace && t.Name == iface.Name[1..]);
            if (implementation != null)
            {
                services.AddScoped(iface, implementation);
            }
        }

        IEnumerable<Type> repoInterfaces = IRepoAssembly.GetTypes()
            .Where(t => t.IsInterface && t.Namespace == repositoryInterfaceNamespace && t.Name.StartsWith(interfacePrefix) && t.Name.EndsWith(repositorySuffix));

        foreach (Type iface in repoInterfaces)
        {
            Type? implementation = repoAssembly.GetTypes()
                .FirstOrDefault(t => t.IsClass && !t.IsAbstract && t.Namespace == repositoryImplNamespace && t.Name == iface.Name[1..]);
            if (implementation != null)
            {   
                services.AddScoped(iface, implementation);
            }
        }
        return services;
    }       
}
