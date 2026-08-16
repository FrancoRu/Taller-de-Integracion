using Application.Utils.Constants.Configuration;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

using System;
using System.IO;
using System.Linq;

namespace Infrastructure.Identity;

/// <summary>
/// Provides a design-time instance of IdentityAppDbContext for EF Core CLI tools.
/// Loads all appsettings*.json files found in the API project directory,
/// so developer-specific files (e.g. appsettings.Franco.json) are picked up automatically.
/// </summary>
public sealed class IdentityAppDbContextFactory : IDesignTimeDbContextFactory<IdentityAppDbContext>
{
    /// <summary>
    /// Builds configuration the same way WebApplication.CreateBuilder does at
    /// runtime: base appsettings.json first, then the environment-specific
    /// file, then every other appsettings.*.json found, with later files
    /// overriding earlier ones (last writer wins).
    /// </summary>
    public IdentityAppDbContext CreateDbContext(string[] args)
    {
        string basePath = ResolveBasePath();

        string aspNetEnv = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
                           ?? "Development";

        ConfigurationBuilder builder = new();
        builder.SetBasePath(basePath);
        builder.AddJsonFile("appsettings.json", optional: true);
        builder.AddJsonFile($"appsettings.{aspNetEnv}.json", optional: true);

        if (Directory.Exists(basePath))
        {
            string?[] extras = Directory
                .GetFiles(basePath, "appsettings.*.json")
                .Select(Path.GetFileName)
                .Where(f => !string.Equals(
                    f, $"appsettings.{aspNetEnv}.json",
                    StringComparison.OrdinalIgnoreCase))
                .ToArray();

            foreach (string? file in extras)
            {
                builder.AddJsonFile(file!, optional: true);
            }
        }

        IConfigurationRoot config = builder.Build();

        string connectionString = config.GetConnectionString(ConfigurationKeys.DbConnection)
            ?? throw new InvalidOperationException(
                $"Connection string '{ConfigurationKeys.DbConnection}' not found.\n" +
                $"Searched in: {basePath}\n" +
                $"Files loaded: appsettings.json, appsettings.{aspNetEnv}.json + all appsettings.*.json");

        DbContextOptionsBuilder<IdentityAppDbContext> optionsBuilder = new();
        optionsBuilder.UseNpgsql(connectionString);

        return new IdentityAppDbContext(optionsBuilder.Options);
    }

    private static string ResolveBasePath()
    {
        string current = Directory.GetCurrentDirectory();

        string[] candidates =
        [
            current,
            Path.Combine(current, "API"),
            Path.Combine(current, "..", "API"),
        ];

        string? match = Array.Find(candidates, path => File.Exists(Path.Combine(path, "appsettings.json")));

        return match is null ? current : Path.GetFullPath(match);
    }
}