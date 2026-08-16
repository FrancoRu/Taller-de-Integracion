using Application.Interfaces.Backup;
using Application.Utils.Constants.Configuration;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using Npgsql;

using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Infrastructure.Backup;

/// <summary>
/// IDatabaseBackupService implementation that shells out to
/// pg_dump via IProcessRunner. Connection details are
/// read from ConnectionStrings:DbConnection (the same Npgsql
/// connection string already used by EF Core) and passed to pg_dump
/// as a discrete argument vector (host/port/user/database flags) — never
/// concatenated into a shell command string. The password is passed via the
/// PGPASSWORD environment variable convention rather than as a
/// command-line argument, so it never appears in the process argument list
/// (which would otherwise be visible via process listings).
/// </summary>
public sealed class PgDumpBackupService(
    IProcessRunner processRunner,
    IConfiguration configuration,
    ILogger<PgDumpBackupService> logger) : IDatabaseBackupService
{
    public async Task<Stream> CreateDumpAsync(CancellationToken ct = default)
    {
        string connectionString = configuration.GetConnectionString(ConfigurationKeys.DbConnection)
            ?? throw new BackupExecutionException(
                $"ConnectionStrings:{ConfigurationKeys.DbConnection} is not configured.");

        string? configuredPgDumpPath = configuration[ConfigurationKeys.Backup.PgDumpPath];
        string pgDumpPath = string.IsNullOrWhiteSpace(configuredPgDumpPath) ? "pg_dump" : configuredPgDumpPath;

        NpgsqlConnectionStringBuilder builder = new(connectionString);

        List<string> args =
        [
            "-h", builder.Host ?? "localhost",
            "-p", builder.Port.ToString(),
            "-U", builder.Username ?? string.Empty,
            "-d", builder.Database ?? string.Empty,
        ];

        Dictionary<string, string>? environmentVariables = null;
        if (!string.IsNullOrEmpty(builder.Password))
        {
            environmentVariables = new Dictionary<string, string>
            {
                ["PGPASSWORD"] = builder.Password,
            };
        }

        ProcessResult result = await processRunner.RunAsync(pgDumpPath, args, environmentVariables, ct);

        if (result.ExitCode != 0)
        {
            logger.LogError(
                "pg_dump failed with exit code {ExitCode}. Path: '{PgDumpPath}'. StdErr: {StdErr}",
                result.ExitCode, pgDumpPath, result.StdErr);

            throw new BackupExecutionException(
                $"pg_dump failed (exit code {result.ExitCode}) using '{pgDumpPath}'. " +
                "Verify pg_dump is installed and on PATH, or set Backup:PgDumpPath to its full path. " +
                $"Details: {result.StdErr}");
        }

        return new MemoryStream(Encoding.UTF8.GetBytes(result.StdOut));
    }
}
