using API.Utils;

using Infrastructure.Identity;
using Infrastructure.Persistance;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace API.Tests;

/// <summary>
/// The self-hosted-Postgres change sets a bounded <c>CommandTimeout</c> on both
/// DbContexts and deliberately does NOT enable retry-on-failure: a retrying
/// execution strategy rejects the raw transactions that
/// <c>DataMaintenanceService</c> opens, and the local DB has no transient
/// network faults for retry to absorb. These tests pin both facts.
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

    private static ServiceProvider BuildProvider(Action<IServiceCollection> register)
    {
        ServiceCollection services = new();
        services.AddSingleton(Configuration());
        register(services);
        return services.BuildServiceProvider();
    }

    private static int? CommandTimeoutOf<TContext>(IServiceProvider provider)
        where TContext : DbContext
    {
        DbContextOptions<TContext> options = provider.GetRequiredService<DbContextOptions<TContext>>();
        return options.Extensions.OfType<RelationalOptionsExtension>().Single().CommandTimeout;
    }

    private static bool RetriesOnFailureOf<TContext>(IServiceProvider provider)
        where TContext : DbContext
    {
        using IServiceScope scope = provider.CreateScope();
        TContext context = scope.ServiceProvider.GetRequiredService<TContext>();
        return context.Database.CreateExecutionStrategy().RetriesOnFailure;
    }

    [Fact]
    public void AddDbContextConfig_SetsCommandTimeout_AndDoesNotRetry()
    {
        using ServiceProvider provider = BuildProvider(s => s.AddDbContextConfig(Configuration()));

        Assert.Equal(30, CommandTimeoutOf<ApplicationDBContext>(provider));
        Assert.False(RetriesOnFailureOf<ApplicationDBContext>(provider));
    }

    [Fact]
    public void AddIdentityConfig_SetsCommandTimeout_AndDoesNotRetry()
    {
        using ServiceProvider provider = BuildProvider(s => s.AddIdentityConfig(Configuration()));

        Assert.Equal(30, CommandTimeoutOf<IdentityAppDbContext>(provider));
        Assert.False(RetriesOnFailureOf<IdentityAppDbContext>(provider));
    }
}
