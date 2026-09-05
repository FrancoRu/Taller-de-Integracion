using Application.Interfaces.Backup;
using Application.Utils.Constants.Configuration;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using Infrastructure.Persistance;

using Npgsql;

using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Infrastructure.Backup;

/// <summary>
/// IDatabaseBackupService implementation that shells out to pg_dump via IProcessRunner, passing connection details as a discrete argument vector instead of a shell command string.
/// </summary>
public sealed class PgDumpBackupService(
    IProcessRunner processRunner,
    IConfiguration configuration,
    ILogger<PgDumpBackupService> logger) : IDatabaseBackupService
{
    /// <summary>
    /// Matches a CREATE, DROP, ALTER, or COMMENT ON EVENT TRIGGER statement so it can be stripped from the dump before restore.
    /// </summary>
    private static readonly Regex EventTriggerStatementPattern = new(
        @"^\s*(CREATE|DROP|ALTER|COMMENT\s+ON)\s+EVENT\s+TRIGGER\b.*?;\s*$",
        RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Singleline | RegexOptions.Compiled);


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
            // These flags shift ownership and role safety to the dump so restoring into a Supabase-managed database with different owners does not fail on CREATE or ALTER OWNER statements.
            "--clean", "--if-exists", "--no-owner", "--no-privileges",
            // Restricting to public and Club12 schemas keeps pg_dump from capturing Supabase-platform-owned objects the app's role doesn't own, which would make --clean's DROP fail on restore.
            // Club12 must be double-quoted because pg_dump's -n pattern matching lowercases an unquoted argument before comparing it against the catalog, and the schema's real name is mixed-case, so an unquoted argument silently matches nothing.
            "-n", "public", "-n", $"\"{EntityConstants.Schema}\"",
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

        int strippedCount = EventTriggerStatementPattern.Matches(result.StdOut).Count;
        string filteredDump = EventTriggerStatementPattern.Replace(result.StdOut, string.Empty);

        if (strippedCount > 0)
        {
            logger.LogInformation(
                "Stripped {Count} EVENT TRIGGER statement(s) from the pg_dump output — these are " +
                "Supabase/PostgREST-managed infrastructure objects the app does not own and cannot " +
                "DROP/CREATE on restore.",
                strippedCount);
        }

        return new MemoryStream(Encoding.UTF8.GetBytes(filteredDump));
    }
}
