using Application.Utils.Constants.Configuration;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

using System;
using System.IO;
using System.Linq;

namespace Infrastructure.Persistance;

/// <summary>
/// Provides a design-time instance of ApplicationDBContext for EF Core CLI tools.
/// Loads all appsettings*.json files found in the API project directory and environment
/// variables, so the EF tooling can build the model without running the API host.
/// </summary>
public sealed class ApplicationDBContextFactory : IDesignTimeDbContextFactory<ApplicationDBContext>
{
    public ApplicationDBContext CreateDbContext(string[] args)
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
            ?? Environment.GetEnvironmentVariable($"ConnectionStrings__{ConfigurationKeys.DbConnection}")
            ?? throw new InvalidOperationException(
                $"Connection string '{ConfigurationKeys.DbConnection}' not found.\n" +
                $"Searched in: {basePath}\n" +
                $"Files loaded: appsettings.json, appsettings.{aspNetEnv}.json + all appsettings.*.json");

        DbContextOptionsBuilder<ApplicationDBContext> optionsBuilder = new();
        optionsBuilder.UseNpgsql(connectionString);

        return new ApplicationDBContext(optionsBuilder.Options)
        {
            Clubs = null!,
            BackupRecords = null!,
            Teams = null!,
            Players = null!,
            Tournaments = null!,
            Divisions = null!,
            DivisionPlayoffMappings = null!,
            TeamPointDeductions = null!,
            Matches = null!,
            MatchSeries = null!,
            PlayersStatistics = null!,
            PlayerSanctions = null!,
            Venues = null!,
            Seasons = null!,
            BlogPosts = null!,
            Stages = null!,
            StageTeamMatches = null!,
            Scorers = null!,
            PlayerTeamRegistrations = null!,
            TeamTournamentRegistrations = null!,
            AuditLogs = null!,
        };
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
