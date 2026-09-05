using API.BackgroundServices;
using API.Utils.Converters;
using API.Utils.Middlewares;

using Application.Backup;
using Application.Interfaces.Backup;
using Application.Interfaces.Mappers;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Application.Interfaces.Storage;
using Application.Services;
using Application.Utils.Constants;
using Application.Utils.Helper.SupabaseHelper;
using Application.Utils.Mappers;

using Domain.Enums;

using FluentEmail.MailKitSmtp;

using Infrastructure.Backup;
using Infrastructure.Email;
using Infrastructure.Identity;
using Infrastructure.Persistance;
using Infrastructure.Repositories;
using Infrastructure.Storage;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

using Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure;

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
        string? connectionString = configuration.GetConnectionString(ConfigurationKeys.DbConnection);
        if (connectionString is null)
        {
            Log.Fatal("Connection string is missing. Using default or fallback connection string.");
            throw new ArgumentException(ErrorMessages.Configuration.ConnectionStringMissing);
        }
        services.AddDbContext<ApplicationDBContext>(options => options.UseNpgsql(connectionString, ConfigureNpgsql));
        services.AddScoped<IClub12DBContext, ApplicationDBContext>();
        services.AddScoped<MedicalRecordSeedBackfiller>();
        services.AddScoped<DataSeeder>();
        services.AddScoped<IDataMaintenanceService, DataMaintenanceService>();

        return services;
    }

    /// <summary>
    /// Shared Npgsql options for every DbContext, so both stay in sync.
    /// A bounded command timeout turns a pathological query into a fast failure
    /// instead of a hung request.
    /// </summary>
    /// <remarks>
    /// Deliberately NOT calling <c>EnableRetryOnFailure</c>: it swaps in
    /// <c>NpgsqlRetryingExecutionStrategy</c>, which rejects any user-initiated
    /// transaction (<c>Database.BeginTransactionAsync</c>) unless the whole unit
    /// runs inside <c>CreateExecutionStrategy().ExecuteAsync(...)</c>.
    /// <see cref="Infrastructure.Repositories.UnitOfWork"/> does that correctly,
    /// but <c>DataMaintenanceService</c> opens a raw transaction and would throw.
    /// The database is now a local container (sub-ms, no transient network
    /// faults), so retry buys almost nothing here anyway.
    /// </remarks>
    private static void ConfigureNpgsql(NpgsqlDbContextOptionsBuilder npgsql)
    {
        npgsql.CommandTimeout(30);
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
                policy.WithOrigins(configuration.GetSection(ConfigurationKeys.AllowedOrigins).Get<string[]>()!)
                      .AllowAnyHeader()
                      .AllowAnyMethod();
            });
        });

        return services;
    }

    /// <summary>
    /// Registers the two container-probe health checks: "self" (tagged
    /// "live") always reports Healthy with no dependency check, and "db"
    /// (tagged "ready") probes ApplicationDBContext connectivity via
    /// AddDbContextCheck, so a misconfigured/unreachable
    /// ConnectionStrings__DbConnection surfaces as not-ready instead of
    /// silently healthy.
    /// </summary>
    public static IServiceCollection AddHealthChecksConfig(this IServiceCollection services)
    {
        services.AddHealthChecks()
                .AddCheck("self", () => HealthCheckResult.Healthy(), tags: ["live"])
                .AddDbContextCheck<ApplicationDBContext>("db", tags: ["ready"]);
        return services;
    }

    /// <summary>
    /// Maps the two container-probe endpoints: `/health` (liveness — only
    /// the "live"-tagged self check) and `/health/ready` (readiness — only
    /// the "ready"-tagged DB check). Both are anonymous: no fallback
    /// authorization policy exists, so this call is defensive.
    /// </summary>
    public static WebApplication MapHealthCheckEndpoints(this WebApplication app)
    {
        app.MapHealthChecks("/health", new HealthCheckOptions
        {
            Predicate = registration => registration.Tags.Contains("live"),
        }).AllowAnonymous();

        app.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = registration => registration.Tags.Contains("ready"),
        }).AllowAnonymous();

        return app;
    }

    /// <summary>
    /// Runs EF migrations for both ApplicationDBContext and IdentityAppDbContext,
    /// then seeds the initial admin user.
    /// </summary>
    public static async Task ExecuteMigrationsAndSeedAsync(this WebApplication app)
    {
        await using AsyncServiceScope scope = app.Services.CreateAsyncScope();

        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        await db.Database.MigrateAsync();

        // HU-99: give every team a stable cross-season club identity. The
        // migration only adds the schema (Clubs table + Team.ClubId); the data
        // backfill runs here as an idempotent step so it links teams created by
        // any migration/seed path. A re-run is a cheap no-op once every team is
        // already linked.
        IClubService clubService = scope.ServiceProvider.GetRequiredService<IClubService>();
        await clubService.BackfillClubsAsync();

        IdentityAppDbContext identityDb = scope.ServiceProvider.GetRequiredService<IdentityAppDbContext>();
        await identityDb.Database.MigrateAsync();

        IdentitySeeder seeder = scope.ServiceProvider.GetRequiredService<IdentitySeeder>();
        await seeder.SeedAsync();

        IConfiguration configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        if (configuration.GetValue<bool>(ConfigurationKeys.Seed.Enabled))
        {
            bool reset = configuration.GetValue<bool>(ConfigurationKeys.Seed.Reset);
            string? logosPath = configuration[ConfigurationKeys.Seed.LogosPath];
            string? medicalRecordPath = configuration[ConfigurationKeys.Seed.MedicalRecordPath];
            int seasons = configuration.GetValue(ConfigurationKeys.Seed.Seasons, 1);
            int playersPerTeam = configuration.GetValue(
                ConfigurationKeys.Seed.PlayersPerTeam, SampleTournamentBuilder.DefaultPlayersPerTeam);
            DataSeeder dataSeeder = scope.ServiceProvider.GetRequiredService<DataSeeder>();
            await dataSeeder.SeedAsync(reset, logosPath, medicalRecordPath, seasons, playersPerTeam);
        }
    }

    /// <summary>
    /// Logs the startup banner once the app is fully configured and about to
    /// start listening for requests.
    /// </summary>
    public static void LogStartupBanner(this WebApplication app)
    {
        Log.Information(LogMessages.StartingUp);
        Log.Information(LogMessages.Banner);
        Log.Information(LogMessages.Started);
    }

    /// <summary>
    /// Configures Swagger middleware for API documentation in non-production environments.
    /// </summary>
    public static IApplicationBuilder UseSwaggerConfig(this IApplicationBuilder app, IHostEnvironment env)
    {
        if (!env.IsProduction())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        return app;
    }

    /// <summary>
    /// Configures exception handling and status code pages middleware.
    /// </summary>
    public static IApplicationBuilder UseExceptionHandlerConfig(this IApplicationBuilder app)
    {
        app.UseStatusCodePages();
        app.UseExceptionHandler();

        return app;
    }

    /// <summary>
    /// One policy per domain role — single source of truth via UserRoleType enum.
    /// </summary>
    private static readonly string[] _roleNames =
    [
        UserRoleType.ADMIN.ToRoleName(),
        UserRoleType.OWNER.ToRoleName(),
        UserRoleType.GUEST.ToRoleName(),
    ];

    /// <summary>
    /// Adds custom authorization policies based on UserRoleType domain roles.
    /// </summary>
    public static IServiceCollection AddCustomAuthorization(this IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            foreach (string role in _roleNames)
            {
                options.AddPolicy(role, policy => policy.RequireRole(role));
            }
        });

        return services;
    }

    /// <summary>
    /// Adds and configures JWT authentication for the application.
    /// </summary>
    public static IServiceCollection AddCustomAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        string? jwtSecret = configuration.GetSection(ConfigurationKeys.Jwt.Key)?.Value;
        if (string.IsNullOrEmpty(jwtSecret))
        {
            throw new ArgumentException(ErrorMessages.Configuration.JwtMissing);
        }

        services.AddAuthentication(options =>
        {
            options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
        }).AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = configuration[ConfigurationKeys.Jwt.Issuer],
                ValidAudience = configuration[ConfigurationKeys.Jwt.Audience],
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret))
            };
        });

        return services;
    }

    /// <summary>
    /// Adds middleware for logging requests to the request context if enabled in configuration.
    /// </summary>
    public static IApplicationBuilder UseLoggingToRequestContextMiddleware(this IApplicationBuilder app, IConfiguration configuration)
    {
        bool useLoggingMiddleware = configuration.GetValue(ConfigurationKeys.UseLoggingMiddleware, false);
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
        const string bearerScheme = "Bearer";
        const string swaggerDocVersion = "v1";

        services.AddSwaggerGen(context =>
        {
            context.SwaggerDoc(swaggerDocVersion, new OpenApiInfo
            {
                Title = configuration[ConfigurationKeys.Swagger.Title],
                Version = configuration[ConfigurationKeys.Swagger.Version],
            });

            // GenerateDocumentationFile is set on all 4 projects (Domain/Application/Infrastructure/API),
            // and their .xml files land next to API's own output via project references — but only API's
            // own file was ever wired in here, so DTO/enum summaries defined outside the API project never
            // reached Swagger's schema view even though they exist and are copied to bin.
            string[] xmlDocAssemblyNames = ["API", "Application", "Domain", "Infrastructure"];
            foreach (string assemblyName in xmlDocAssemblyNames)
            {
                string xmlPath = Path.Combine(AppContext.BaseDirectory, $"{assemblyName}.xml");
                if (File.Exists(xmlPath))
                {
                    context.IncludeXmlComments(xmlPath);
                }
            }

            context.ResolveConflictingActions(apiDescriptions => apiDescriptions.First());

            context.AddSecurityDefinition(bearerScheme, new OpenApiSecurityScheme
            {
                Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token in the text input below.",
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.ApiKey,
                Scheme = bearerScheme
            });

            context.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = bearerScheme },
                        Scheme    = "oauth2",
                        Name      = bearerScheme,
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

        // Medical-record file storage (HU-55/HU-56). Reuses the shared Supabase
        // client via ISupabaseRawStorage (implemented by SupabaseHelper) and
        // confines files to their own medical-records/ area, separate from the
        // backups/ area. Registered here (not conditionally like backup
        // storage) because medical-record uploads always target Supabase.
        services.AddSingleton<ISupabaseRawStorage>(sp => sp.GetRequiredService<SupabaseHelper>());
        services.AddSingleton<IMedicalRecordStorage, SupabaseMedicalRecordStorage>();
        return services;
    }

    /// <summary>
    /// Registers Identity DbContext, ASP.NET Core Identity services, IAuthenticationService,
    /// IUserManagementService, and IdentitySeeder.
    /// </summary>
    public static IServiceCollection AddIdentityConfig(
        this IServiceCollection services, IConfiguration configuration)
    {
        string? connectionString = configuration.GetConnectionString(ConfigurationKeys.DbConnection);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new ArgumentException(ErrorMessages.Configuration.ConnectionStringMissing);
        }

        services.AddDbContext<IdentityAppDbContext>(options => options.UseNpgsql(connectionString, ConfigureNpgsql));

        services.AddIdentityCore<ApplicationUser>(options =>
        {
            options.User.RequireUniqueEmail = true;
        })
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<IdentityAppDbContext>()
            .AddSignInManager()
            .AddDefaultTokenProviders();

        services.AddScoped<IAuthenticationService, IdentityAuthenticationService>();
        services.AddScoped<IUserManagementService, IdentityUserManagementService>();
        services.AddScoped<IdentitySeeder>();

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
        string? mapperInterfaceNamespace = typeof(IScorerMapper).Namespace;
        string? mapperImplNamespace = typeof(ScorerMapper).Namespace;
        string serviceSuffix = "Service";
        string repositorySuffix = "Repository";
        string mapperSuffix = "Mapper";

        ArgumentNullException.ThrowIfNull(serviceInterfaceNamespace);
        ArgumentNullException.ThrowIfNull(serviceImplNamespace);
        ArgumentNullException.ThrowIfNull(repositoryInterfaceNamespace);
        ArgumentNullException.ThrowIfNull(repositoryImplNamespace);
        ArgumentNullException.ThrowIfNull(mapperInterfaceNamespace);
        ArgumentNullException.ThrowIfNull(mapperImplNamespace);

        Assembly serviceAssembly = typeof(AuthService).Assembly;
        Assembly IServiceAssembly = typeof(IAuthService).Assembly;
        Assembly repoAssembly = typeof(BlogPostRepository).Assembly;
        Assembly IRepoAssembly = typeof(IBlogPostRepository).Assembly;
        Assembly mapperAssembly = typeof(ScorerMapper).Assembly;
        Assembly IMapperAssembly = typeof(IScorerMapper).Assembly;

        services.HelperRegisterScoped(serviceAssembly, IServiceAssembly, serviceSuffix, serviceInterfaceNamespace, serviceImplNamespace);
        services.HelperRegisterScoped(repoAssembly, IRepoAssembly, repositorySuffix, repositoryInterfaceNamespace, repositoryImplNamespace);
        services.HelperRegisterScoped(mapperAssembly, IMapperAssembly, mapperSuffix, mapperInterfaceNamespace, mapperImplNamespace);


        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Audit trail (HU-101): resolve the current caller's identity from the
        // HTTP context so application/infrastructure services can record "who"
        // without depending on the web layer.
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserAccessor, API.Utils.Helpers.HttpCurrentUserAccessor>();

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
            Type? implementation = Array.Find(implementationAssembly.GetTypes(),
                t => t.IsClass && !t.IsAbstract
                     && t.Namespace == implNamespace
                     && t.Name == iface.Name[1..]);

            if (implementation is not null)
            {
                services.AddScoped(iface, implementation);
            }
        }
    }
    /// <summary>
    /// Registers FluentEmail with Mailgun and binds IEmailService
    /// to FluentEmailHelper.
    /// </summary>
    public static IServiceCollection AddEmailConfig(
       this IServiceCollection services, IConfiguration configuration)
    {
        string smtpHost = configuration[ConfigurationKeys.Smtp.Host]
            ?? throw new ArgumentException(ErrorMessages.Configuration.SmtpKeyMissing(ConfigurationKeys.Smtp.Host));
        int smtpPort = int.Parse(configuration[ConfigurationKeys.Smtp.Port]
            ?? throw new ArgumentException(ErrorMessages.Configuration.SmtpKeyMissing(ConfigurationKeys.Smtp.Port)));
        string username = configuration[ConfigurationKeys.Smtp.Username]
            ?? throw new ArgumentException(ErrorMessages.Configuration.SmtpKeyMissing(ConfigurationKeys.Smtp.Username));
        string password = configuration[ConfigurationKeys.Smtp.Password]
            ?? throw new ArgumentException(ErrorMessages.Configuration.SmtpKeyMissing(ConfigurationKeys.Smtp.Password));
        bool useSsl = configuration.GetValue(ConfigurationKeys.Smtp.UseSsl, true);
        string fromEmail = configuration[ConfigurationKeys.Smtp.FromEmail] ?? "noreply@club12.com";
        string fromName = configuration[ConfigurationKeys.Smtp.FromName] ?? "Club12";

        services
            .AddFluentEmail(fromEmail, fromName)
            .AddMailKitSender(new SmtpClientOptions
            {
                Server = smtpHost,
                Port = smtpPort,
                UseSsl = useSsl,
                User = username,
                Password = password,
                RequiresAuthentication = true
            });

        services.AddScoped<IEmailService, FluentEmailHelper>();

        return services;
    }

    /// <summary>
    /// Registers the database backup feature: binds the
    /// Backup config section into a BackupOptions
    /// singleton and registers its ports/adapters. Most of this is
    /// singleton (pg_dump adapter, psql restore adapter, retention
    /// policy, storage adapter, the process-wide
    /// BackupOperationLock and IMaintenanceModeState, and
    /// DatabaseBackupHostedService itself) — the exceptions are
    /// IBackupCatalog (EF-backed, needs the scoped
    /// ApplicationDBContext) and IBackupOperationsService (depends
    /// on the catalog), both registered scoped so the manual endpoint
    /// (BackupController) and the scheduled job (resolving from a
    /// DI scope per tick) each get their own catalog/DbContext instance
    /// while still serializing through the one shared
    /// BackupOperationLock singleton.
    /// The IBackupStorage implementation is selected by
    /// BackupOptions.StorageTarget: Supabase registers
    /// SupabaseBackupStorage (reusing the existing
    /// SupabaseHelper singleton as its raw-storage boundary);
    /// anything else (including the default, Local) registers
    /// LocalDirectoryBackupStorage.
    /// </summary>
    /// <remarks>
    /// Deliberately NOT registered via RegisterScoped's
    /// reflection scan: these live outside the
    /// Application.Interfaces.Services namespace on purpose, because
    /// that scanner auto-binds everything it finds as <b>Scoped</b> — a
    /// lifetime mismatch for the singleton services registered here (and,
    /// for IBackupCatalog/IBackupOperationsService, explicit scoped
    /// registration is clearer than relying on naming-convention reflection).
    /// </remarks>
    public static IServiceCollection AddBackupConfig(this IServiceCollection services, IConfiguration configuration)
    {
        BackupOptions options = configuration.GetSection(ConfigurationKeys.Backup.Section).Get<BackupOptions>() ?? new BackupOptions();
        services.AddSingleton(options);

        services.AddSingleton<IBackupRetentionPolicy, KeepLastNRetentionPolicy>();
        services.AddSingleton<IProcessRunner, ProcessRunner>();
        services.AddSingleton<IDatabaseBackupService, PgDumpBackupService>();
        services.AddSingleton<IDatabaseRestoreService, PsqlDatabaseRestoreService>();
        services.AddSingleton<IMaintenanceModeState, MaintenanceModeState>();
        services.AddSingleton<BackupOperationLock>();

        if (string.Equals(options.StorageTarget, BackupStorageTargets.Supabase, StringComparison.OrdinalIgnoreCase))
        {
            services.AddSingleton<ISupabaseRawStorage>(sp => sp.GetRequiredService<SupabaseHelper>());
            services.AddSingleton<IBackupStorage, SupabaseBackupStorage>();
        }
        else
        {
            services.AddSingleton<IBackupStorage>(sp => new LocalDirectoryBackupStorage(
                options.LocalStoragePath, sp.GetRequiredService<ILogger<LocalDirectoryBackupStorage>>()));
        }

        services.AddScoped<IBackupCatalog, EfBackupCatalog>();
        services.AddScoped<IBackupOperationsService, BackupOperationsService>();

        services.AddSingleton<DatabaseBackupHostedService>();

        return services;
    }
}
