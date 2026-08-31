using API.Utils;

using Infrastructure.Identity;
using Infrastructure.Persistance;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace API.Tests;

/// <summary>
/// The self-hosted-Postgres change hardens the Npgsql registrations so a brief
/// database outage during a container restart or cutover fails fast and retries
/// instead of hanging a request. Both DbContexts must carry the same options.
///
/// Asserted through the public <see cref="RelationalOptionsExtension"/> base
/// (the Npgsql extension derives from it) rather than the provider's internal
/// options type.
/// </summary>
public class NpgsqlOptionsTests
{
    private const string PostgresConnectionString =
        "Host=db;Port=5432;Database=postgres;Username=postgres;Password=x;SSL Mode=Disable";

    private static IConfiguration Configuration() =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DbConnection"] = PostgresConnectionString,
            })
            .Build();

    private static RelationalOptionsExtension RelationalExtensionFor<TContext>(IServiceProvider provider)
        where TContext : DbContext
    {
        DbContextOptions<TContext> options = provider.GetRequiredService<DbContextOptions<TContext>>();
        return options.Extensions.OfType<RelationalOptionsExtension>().Single();
    }

    [Fact]
    public void AddDbContextConfig_EnablesRetryAndCommandTimeout_OnApplicationDbContext()
    {
        ServiceCollection services = new();
        services.AddSingleton(Configuration());
        services.AddDbContextConfig(Configuration());

        using ServiceProvider provider = services.BuildServiceProvider();
        RelationalOptionsExtension extension = RelationalExtensionFor<ApplicationDBContext>(provider);

        Assert.NotNull(extension.ExecutionStrategyFactory);
        Assert.Equal(30, extension.CommandTimeout);
    }

    [Fact]
    public void AddIdentityConfig_EnablesRetryAndCommandTimeout_OnIdentityDbContext()
    {
        ServiceCollection services = new();
        services.AddSingleton(Configuration());
        services.AddIdentityConfig(Configuration());

        using ServiceProvider provider = services.BuildServiceProvider();
        RelationalOptionsExtension extension = RelationalExtensionFor<IdentityAppDbContext>(provider);

        Assert.NotNull(extension.ExecutionStrategyFactory);
        Assert.Equal(30, extension.CommandTimeout);
    }
}
