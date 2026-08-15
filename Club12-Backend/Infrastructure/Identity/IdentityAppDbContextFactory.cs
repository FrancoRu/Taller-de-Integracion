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
    public IdentityAppDbContext CreateDbContext(string[] args)
    {
        string basePath = ResolveBasePath();

        // Load base settings first, then let any *.json file override (last writer wins).
        // This mirrors what WebApplication.CreateBuilder does at runtime.
        string aspNetEnv = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
                           ?? "Development";

        ConfigurationBuilder builder = new();
        builder.SetBasePath(basePath);
        builder.AddJsonFile("appsettings.json",                       optional: true);
        builder.AddJsonFile($"appsettings.{aspNetEnv}.json",         optional: true);

        // Scan every remaining appsettings.*.json (catches appsettings.Franco.json, etc.)
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
                builder.AddJsonFile(file!, optional: true);
        }

        IConfigurationRoot config = builder.Build();

        string connectionString = config.GetConnectionString("DbConnection")
            ?? throw new InvalidOperationException(
                $"Connection string 'DbConnection' not found.\n" +
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

        foreach (string path in candidates)
        {
            if (File.Exists(Path.Combine(path, "appsettings.json")))
                return Path.GetFullPath(path);
        }

        return current;
    }
}