using Application.Interfaces.Backup;
using Application.Utils.Constants.Configuration;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using Npgsql;

using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Infrastructure.Backup;

/// <summary>
/// IDatabaseRestoreService implementation that shells out to <c>psql</c> via
/// IProcessRunner, mirroring how PgDumpBackupService shells out to
/// <c>pg_dump</c>: connection details come from
/// ConnectionStrings:DbConnection and are passed as a discrete argument
/// vector (host/port/user/database flags) — never concatenated into a shell
/// string — and the password goes through the PGPASSWORD environment variable
/// so it never appears in the process argument list.
///
/// The dump bytes are written to a short-lived temp file (the "restore copy"
/// working file) and fed to <c>psql -f &lt;file&gt;</c> with
/// <c>ON_ERROR_STOP=1</c>, so a mid-restore SQL error aborts with a non-zero
/// exit instead of silently leaving a half-applied database. The temp file is
/// always deleted before returning, whether the restore succeeds or fails.
/// A non-zero exit (or a missing binary, which the ProcessRunner surfaces as
/// exit code -1) is turned into a BackupExecutionException.
/// </summary>
public sealed class PgRestoreService(
    IProcessRunner processRunner,
    IConfiguration configuration,
    ILogger<PgRestoreService> logger) : IDatabaseRestoreService
{
    public async Task RestoreAsync(Stream dumpContent, CancellationToken ct = default)
    {
        string connectionString = configuration.GetConnectionString(ConfigurationKeys.DbConnection)
            ?? throw new BackupExecutionException(
                $"ConnectionStrings:{ConfigurationKeys.DbConnection} is not configured.");

        string? configuredPsqlPath = configuration[ConfigurationKeys.Backup.PsqlPath];
        string psqlPath = string.IsNullOrWhiteSpace(configuredPsqlPath) ? "psql" : configuredPsqlPath;

        NpgsqlConnectionStringBuilder builder = new(connectionString);

        string restoreCopyPath = Path.Combine(Path.GetTempPath(), $"club12-restore-{Path.GetRandomFileName()}.sql");

        try
        {
            await using (FileStream file = File.Create(restoreCopyPath))
            {
                await dumpContent.CopyToAsync(file, ct);
            }

            List<string> args =
            [
                "-h", builder.Host ?? "localhost",
                "-p", builder.Port.ToString(),
                "-U", builder.Username ?? string.Empty,
                "-d", builder.Database ?? string.Empty,
                "-v", "ON_ERROR_STOP=1",
                "-f", restoreCopyPath,
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
        finally
        {
            TryDeleteRestoreCopy(restoreCopyPath);
        }
    }

    private void TryDeleteRestoreCopy(string restoreCopyPath)
    {
        try
        {
            if (File.Exists(restoreCopyPath))
            {
                File.Delete(restoreCopyPath);
            }
        }
        catch (IOException ex)
        {
            logger.LogWarning(ex, "Failed to delete transient restore copy {Path}.", restoreCopyPath);
        }
    }
}
