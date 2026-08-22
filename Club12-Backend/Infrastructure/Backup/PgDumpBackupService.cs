using Application.Interfaces.Backup;
using Application.Utils.Constants.Configuration;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using Npgsql;

using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
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
    /// <summary>
    /// Matches a single CREATE/DROP/ALTER/COMMENT ON EVENT TRIGGER statement
    /// (event triggers are database-wide, not schema-scoped, so pg_dump
    /// captures them regardless of any schema filter). On a Supabase-managed
    /// database these are always platform-internal objects (PostgREST's
    /// schema-cache-reload hooks, pgsodium's mask-update hook, etc.) owned by
    /// a role the app's connection never is — restoring them via --clean's
    /// DROP EVENT TRIGGER fails with "must be owner of event trigger". The
    /// app defines no event triggers of its own, so every match here is safe
    /// to drop from the dump entirely.
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
            // Restore uses psql -f against a plain-SQL dump (design.md's
            // "Keep plain-SQL dumps; restore with psql" decision); these
            // flags move the ownership/role safety onto the dump side so
            // restoring into a Supabase-managed database (different
            // owners/roles) does not fail on CREATE/ALTER OWNER statements.
            "--clean", "--if-exists", "--no-owner", "--no-privileges",
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

        int strippedCount = 0;
        string filteredDump = EventTriggerStatementPattern.Replace(result.StdOut, _ =>
        {
            strippedCount++;
            return string.Empty;
        });

        // False positive: Regex.Replace(string, MatchEvaluator) runs the evaluator
        // synchronously per match before returning, so strippedCount is fully
        // updated here — Sonar's dataflow analysis doesn't model that.
#pragma warning disable S2583
        if (strippedCount > 0)
        {
            logger.LogInformation(
                "Stripped {Count} EVENT TRIGGER statement(s) from the pg_dump output — these are " +
                "Supabase/PostgREST-managed infrastructure objects the app does not own and cannot " +
                "DROP/CREATE on restore.",
                strippedCount);
        }
#pragma warning restore S2583

        return new MemoryStream(Encoding.UTF8.GetBytes(filteredDump));
    }
}
