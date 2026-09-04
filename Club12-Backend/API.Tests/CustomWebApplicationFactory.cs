using Infrastructure.Identity;
using Infrastructure.Persistance;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;

namespace API.Tests;

/// <summary>
/// Boots the real API host for integration tests, replacing the
/// production Npgsql-backed ApplicationDBContext and
/// IdentityAppDbContext registrations with SQLite in-memory
/// connections. No production code path is altered by this factory; it only
/// intercepts service registration for the test host.
///
/// The host's own startup code (ExecuteMigrationsAndSeedAsync) still
/// runs for real and calls Database.MigrateAsync() against these
/// SQLite connections — replaying the checked-in migration history (written
/// for Npgsql) against SQLite is unreliable (schema-qualified raw SQL,
/// provider-specific column types). To keep the harness reliable, schema is
/// instead built once via EnsureCreated() directly from the current
/// model, and the EF migrations-history table is pre-seeded so the host's
/// own MigrateAsync() call sees nothing pending and becomes a no-op.
/// </summary>
public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _appConnection;
    private readonly SqliteConnection _identityConnection;

    /// <summary>
    /// Sets these environment variables first because startup config binding
    /// (ConnectionStrings, AllowedOrigins, JWT, Smtp) throws before the host is
    /// even built otherwise — these values are read from configuration before
    /// WebApplicationFactory gets a chance to override services. The two SQLite
    /// connections are then kept open for the lifetime of the factory, since an
    /// in-memory SQLite database is destroyed as soon as its connection closes.
    /// </summary>
    public CustomWebApplicationFactory()
    {
        Environment.SetEnvironmentVariable("ConnectionStrings__DbConnection", "Host=localhost;Database=club12_test;Username=test;Password=test");
        Environment.SetEnvironmentVariable("AllowedOrigins__0", "http://localhost:5173");
        Environment.SetEnvironmentVariable("JWT__Key", "test-signing-key-at-least-32-characters-long");
        Environment.SetEnvironmentVariable("JWT__Issuer", "club12-tests");
        Environment.SetEnvironmentVariable("JWT__Audience", "club12-tests");
        Environment.SetEnvironmentVariable("Smtp__Host", "localhost");
        Environment.SetEnvironmentVariable("Smtp__Port", "25");
        Environment.SetEnvironmentVariable("Smtp__Username", "test");
        Environment.SetEnvironmentVariable("Smtp__Password", "test");
        Environment.SetEnvironmentVariable("Smtp__UseSsl", "false");
        // appsettings.json ships with Seed:Enabled=true, which makes the host's
        // ExecuteMigrationsAndSeedAsync run the full sample seed — that path
        // constructs SupabaseHelper, whose Supabase client ctor throws on the
        // null ProjectUrl the test host has no config for, so the host never
        // builds. Tests seed their own data; keep the sample seed off.
        Environment.SetEnvironmentVariable("Seed__Enabled", "false");

        _appConnection = new SqliteConnection("DataSource=:memory:");
        _appConnection.Open();

        _identityConnection = new SqliteConnection("DataSource=:memory:");
        _identityConnection.Open();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            ReplaceDbContext<ApplicationDBContext>(services, _appConnection);
            ReplaceDbContext<IdentityAppDbContext>(services, _identityConnection);
        });
    }

    private static void ReplaceDbContext<TContext>(IServiceCollection services, SqliteConnection connection)
        where TContext : DbContext
    {
        ServiceDescriptor? descriptor = services.SingleOrDefault(
            d => d.ServiceType == typeof(DbContextOptions<TContext>));
        if (descriptor is not null)
        {
            services.Remove(descriptor);
        }

        services.AddDbContext<TContext>(options => options.UseSqlite(connection));

        using ServiceProvider provider = services.BuildServiceProvider();
        using IServiceScope scope = provider.CreateScope();
        TContext db = scope.ServiceProvider.GetRequiredService<TContext>();

        db.Database.EnsureCreated();
        MarkAllMigrationsAsApplied(db);
    }

    /// <summary>
    /// Pre-seeds the EF Core migrations-history table so that the host's own
    /// startup Database.MigrateAsync() call finds nothing pending and
    /// performs no schema changes against the SQLite database we already
    /// built via DatabaseFacade.EnsureCreated.
    /// </summary>
    private static void MarkAllMigrationsAsApplied(DbContext db)
    {
        IHistoryRepository historyRepository = db.GetService<IHistoryRepository>();

        if (!historyRepository.Exists())
        {
            db.Database.ExecuteSqlRaw(historyRepository.GetCreateScript());
        }

        foreach (string migrationId in db.Database.GetMigrations())
        {
            string insertScript = historyRepository.GetInsertScript(
                new HistoryRow(migrationId, ProductInfo.GetVersion()));
            db.Database.ExecuteSqlRaw(insertScript);
        }
    }

    /// <summary>
    /// Test-only hook for readiness-check tests: closes the SQLite
    /// connection backing ApplicationDBContext and repoints it at a
    /// nonexistent path with `Mode=ReadWrite` (no auto-create), so any
    /// subsequent reopen attempt fails with SQLITE_CANTOPEN instead of
    /// silently succeeding. A plain Close()/Dispose() is not enough here:
    /// unlike most ADO.NET providers, SqliteConnection happily reopens an
    /// `:memory:` connection after Dispose() by creating a brand-new, empty
    /// in-memory database — which would make CanConnectAsync report
    /// "reachable" again instead of simulating a real outage. Call this
    /// AFTER the host has booted (e.g. after CreateClient()) so schema
    /// creation already ran against the original in-memory database.
    /// </summary>
    public void BreakDatabaseConnection()
    {
        _appConnection.Close();
        _appConnection.ConnectionString =
            "Data Source=./health-endpoint-tests-unreachable/does-not-exist.db;Mode=ReadWrite";
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            _appConnection.Dispose();
            _identityConnection.Dispose();
        }
    }
}
