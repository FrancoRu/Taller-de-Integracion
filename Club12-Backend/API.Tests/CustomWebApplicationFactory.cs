using System;
using System.Linq;
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
/// Boots the real <c>API</c> host for integration tests, replacing the
/// production Npgsql-backed <see cref="ApplicationDBContext"/> and
/// <see cref="IdentityAppDbContext"/> registrations with SQLite in-memory
/// connections. No production code path is altered by this factory; it only
/// intercepts service registration for the test host.
///
/// The host's own startup code (<c>ExecuteMigrationsAndSeedAsync</c>) still
/// runs for real and calls <c>Database.MigrateAsync()</c> against these
/// SQLite connections — replaying the checked-in migration history (written
/// for Npgsql) against SQLite is unreliable (schema-qualified raw SQL,
/// provider-specific column types). To keep the harness reliable, schema is
/// instead built once via <c>EnsureCreated()</c> directly from the current
/// model, and the EF migrations-history table is pre-seeded so the host's
/// own <c>MigrateAsync()</c> call sees nothing pending and becomes a no-op.
/// </summary>
public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _appConnection;
    private readonly SqliteConnection _identityConnection;

    public CustomWebApplicationFactory()
    {
        // Required, otherwise startup config binding (ConnectionStrings,
        // AllowedOrigins, JWT, Smtp) throws before the host is even built —
        // these values are read from configuration before WebApplicationFactory
        // gets a chance to override services.
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

        // Keep one open connection per context alive for the lifetime of the
        // factory — an in-memory SQLite database is destroyed when its
        // connection closes.
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
    /// startup <c>Database.MigrateAsync()</c> call finds nothing pending and
    /// performs no schema changes against the SQLite database we already
    /// built via <see cref="DatabaseFacade.EnsureCreated"/>.
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
