using Application.Interfaces.Backup;
using Application.Utils.Constants.Configuration;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using Npgsql;

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Infrastructure.Backup;

/// <summary>
/// IDatabaseRestoreService implementation that shells out to psql via IProcessRunner because pg_dump produces plain SQL, which only psql can replay.
/// </summary>
public sealed class PsqlDatabaseRestoreService(
    IProcessRunner processRunner,
    IConfiguration configuration,
    ILogger<PsqlDatabaseRestoreService> logger) : IDatabaseRestoreService
{
    public async Task RestoreAsync(string dumpFilePath, CancellationToken ct = default)
    {
        string connectionString = configuration.GetConnectionString(ConfigurationKeys.DbConnection)
            ?? throw new BackupExecutionException(
                $"ConnectionStrings:{ConfigurationKeys.DbConnection} is not configured.");

        string? configuredPsqlPath = configuration[ConfigurationKeys.Backup.PsqlPath];
        string psqlPath = string.IsNullOrWhiteSpace(configuredPsqlPath) ? "psql" : configuredPsqlPath;

        NpgsqlConnectionStringBuilder builder = new(connectionString);

        List<string> args =
        [
            "-h", builder.Host ?? "localhost",
            "-p", builder.Port.ToString(),
            "-U", builder.Username ?? string.Empty,
            "-d", builder.Database ?? string.Empty,
            // ON_ERROR_STOP=1 turns the first SQL error inside the dump into
            // a non-zero psql exit code instead of silently continuing past
            // it, so a partial/failed restore is detected rather than
            // reported as success.
            "-v", "ON_ERROR_STOP=1",
            "-f", dumpFilePath,
        ];

        Dictionary<string, string>? environmentVariables = null;
        if (!string.IsNullOrEmpty(builder.Password))
        {
            environmentVariables = new Dictionary<string, string>
            {
                ["PGPASSWORD"] = builder.Password,
            };
        }

        ProcessResult result = await processRunner.RunAsync(psqlPath, args, environmentVariables, ct);

        if (result.ExitCode != 0)
        {
            logger.LogError(
                "psql restore failed with exit code {ExitCode}. Path: '{PsqlPath}'. StdErr: {StdErr}",
                result.ExitCode, psqlPath, result.StdErr);

            throw new BackupExecutionException(
                $"psql restore failed (exit code {result.ExitCode}) using '{psqlPath}'. " +
                "Verify psql is installed and on PATH, or set Backup:PsqlPath to its full path. " +
                $"Details: {result.StdErr}");
        }
    }
}
