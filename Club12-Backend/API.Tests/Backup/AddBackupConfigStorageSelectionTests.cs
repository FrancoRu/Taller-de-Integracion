using System.Collections.Generic;
using System.Linq;
using API.Utils;
using Application.Interfaces.Backup;
using Infrastructure.Backup;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace API.Tests.Backup;

/// <summary>
/// Tests StartupExtensions.AddBackupConfig's storage-target
/// branching by inspecting the resulting ServiceDescriptor for
/// IBackupStorage — never calling BuildServiceProvider
/// or resolving anything. This keeps the test free of real Supabase network
/// I/O (a SupabaseBackupStorage registration is type-based, so
/// its ServiceDescriptor.ImplementationType is inspectable
/// without constructing it) and free of local-filesystem side effects (a
/// LocalDirectoryBackupStorage registration is factory-based and is
/// never invoked here either).
/// </summary>
public sealed class AddBackupConfigStorageSelectionTests
{
    private static IServiceCollection BuildServices(string storageTarget)
    {
        Dictionary<string, string?> values = new()
        {
            ["Backup:Enabled"] = "false",
            ["Backup:StorageTarget"] = storageTarget,
            ["Backup:LocalStoragePath"] = "backups",
            ["Backup:PgDumpPath"] = "pg_dump",
        };
        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(values).Build();

        ServiceCollection services = new();
        services.AddBackupConfig(configuration);
        return services;
    }

    [Fact]
    public void AddBackupConfig_StorageTargetSupabase_RegistersSupabaseBackupStorage()
    {
        IServiceCollection services = BuildServices("Supabase");

        ServiceDescriptor descriptor = services.Single(d => d.ServiceType == typeof(IBackupStorage));

        Assert.Equal(typeof(SupabaseBackupStorage), descriptor.ImplementationType);
    }

    [Theory]
    [InlineData("supabase")]
    [InlineData("SUPABASE")]
    [InlineData("SupaBase")]
    public void AddBackupConfig_StorageTargetSupabase_CaseInsensitive_RegistersSupabaseBackupStorage(string storageTarget)
    {
        IServiceCollection services = BuildServices(storageTarget);

        ServiceDescriptor descriptor = services.Single(d => d.ServiceType == typeof(IBackupStorage));

        Assert.Equal(typeof(SupabaseBackupStorage), descriptor.ImplementationType);
    }

    [Theory]
    [InlineData("Local")]
    [InlineData("")]
    [InlineData("SomethingElse")]
    public void AddBackupConfig_StorageTargetNotSupabase_RegistersLocalDirectoryBackupStorage_NotSupabase(string storageTarget)
    {
        IServiceCollection services = BuildServices(storageTarget);

        ServiceDescriptor descriptor = services.Single(d => d.ServiceType == typeof(IBackupStorage));

        // Local storage is registered via a factory (it needs the plain
        // LocalStoragePath string, not a DI-resolvable type), so we assert
        // it is NOT the type-based Supabase registration rather than
        // invoking the factory (which would touch the local filesystem).
        Assert.NotEqual(typeof(SupabaseBackupStorage), descriptor.ImplementationType);
        Assert.Null(descriptor.ImplementationType);
        Assert.NotNull(descriptor.ImplementationFactory);
    }
}
