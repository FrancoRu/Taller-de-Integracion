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
            // Restore uses psql -f against a plain-SQL dump (design.md's
            // "Keep plain-SQL dumps; restore with psql" decision); these
            // flags move the ownership/role safety onto the dump side so
            // restoring into a Supabase-managed database (different
            // owners/roles) does not fail on CREATE/ALTER OWNER statements.
            "--clean", "--if-exists", "--no-owner", "--no-privileges",
            // Restrict the dump to schemas the app actually owns: "public"
            // (ASP.NET Core Identity's default, unconfigured schema) and
            // "Club12" (every domain entity — EntityConstants.Schema).
            // Without this, pg_dump captures the WHOLE database, including
            // Supabase-platform-owned tables/views/functions the app's
            // connection role never owns — --clean's DROP for those fails on
            // restore with "must be owner of ...". Event triggers are
            // database-wide, not schema-scoped, so this does NOT exclude
            // them; see EventTriggerStatementPattern below for that.
            //
            // "Club12" MUST be double-quoted here: pg_dump's -n pattern
            // matching folds an unquoted pattern to lowercase before
            // comparing against the catalog, and this schema's real name is
            // mixed-case, so a bare "Club12" argument silently matches
            // nothing and pg_dump dumps zero tables from it — confirmed by
            // inspecting a real production dump taken with the unquoted
            // form, which contained only "public" content. No shell is
            // involved (args go straight into the process argument vector),
            // so this is a literal-quote data value, not shell quoting.
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
