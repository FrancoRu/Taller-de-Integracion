using API.Tests.Backup.Fakes;

using Application.Interfaces.Backup;

using Infrastructure.Backup;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace API.Tests.Backup;

/// <summary>
/// Unit tests for PsqlDatabaseRestoreService using a
/// FakeProcessRunner — no real psql binary involved.
/// Covers: correct argument-vector construction (design.md's
/// "psql -v ON_ERROR_STOP=1 -f &lt;tmp&gt;" decision — plain-SQL restore, not
/// pg_restore), subprocess argument-injection resistance for a
/// dump-file path crafted to look like shell/psql-flag injection, and
/// failure handling (non-zero exit / missing binary → logged, handled
/// exception, not a crash).
/// </summary>
public class PsqlDatabaseRestoreServiceTests
{
    private static IConfiguration BuildConfiguration(string connectionString, string? psqlPath = null)
    {
        Dictionary<string, string?> values = new()
        {
            ["ConnectionStrings:DbConnection"] = connectionString,
        };
        if (psqlPath is not null)
        {
            values["Backup:PsqlPath"] = psqlPath;
        }

        return new ConfigurationBuilder().AddInMemoryCollection(values).Build();
    }

    [Fact]
    public async Task RestoreAsync_SuccessfulExit_CompletesWithoutThrowing()
    {
        FakeProcessRunner runner = new() { ResultToReturn = new ProcessResult(0, string.Empty, string.Empty) };
        IConfiguration configuration = BuildConfiguration(
            "Host=localhost;Port=5432;Database=club12;Username=app;Password=secret");
        PsqlDatabaseRestoreService service = new(runner, configuration, NullLogger<PsqlDatabaseRestoreService>.Instance);

        await service.RestoreAsync("/tmp/backup-restore-test.sql");

        Assert.Equal(1, runner.CallCount);
    }

    /// <summary>
    /// The password must never appear in the argument vector, since it would
    /// otherwise leak via `ps`/task list; it is instead passed through the
    /// PGPASSWORD environment variable — identical convention to
    /// PgDumpBackupService.
    /// </summary>
    [Fact]
    public async Task RestoreAsync_BuildsArgumentVectorFromConnectionString_PasswordNeverInArgs()
    {
        FakeProcessRunner runner = new() { ResultToReturn = new ProcessResult(0, string.Empty, string.Empty) };
        IConfiguration configuration = BuildConfiguration(
            "Host=db.internal;Port=5433;Database=club12;Username=app_user;Password=s3cret");
        PsqlDatabaseRestoreService service = new(runner, configuration, NullLogger<PsqlDatabaseRestoreService>.Instance);

        await service.RestoreAsync("/tmp/backup-restore-test.sql");

        Assert.NotNull(runner.CapturedArgs);
        Assert.Contains("db.internal", runner.CapturedArgs!);
        Assert.Contains("5433", runner.CapturedArgs!);
        Assert.Contains("app_user", runner.CapturedArgs!);
        Assert.Contains("club12", runner.CapturedArgs!);
        Assert.DoesNotContain("s3cret", runner.CapturedArgs!);
        Assert.NotNull(runner.CapturedEnvironmentVariables);
        Assert.Equal("s3cret", runner.CapturedEnvironmentVariables!["PGPASSWORD"]);
    }

    /// <summary>
    /// Asserts the exact arg vector for design.md's chosen restore invocation:
    /// "-v", "ON_ERROR_STOP=1", "-f", &lt;dumpFilePath&gt; — ON_ERROR_STOP=1 is what
    /// turns the first SQL error inside the dump into a non-zero psql exit
    /// code instead of silently continuing past it.
    /// </summary>
    [Fact]
    public async Task RestoreAsync_UsesPsqlWithOnErrorStopAndDumpFilePathFlag()
    {
        FakeProcessRunner runner = new() { ResultToReturn = new ProcessResult(0, string.Empty, string.Empty) };
        IConfiguration configuration = BuildConfiguration(
            "Host=localhost;Port=5432;Database=club12;Username=app;Password=x", psqlPath: "/usr/bin/psql");
        PsqlDatabaseRestoreService service = new(runner, configuration, NullLogger<PsqlDatabaseRestoreService>.Instance);

        await service.RestoreAsync("/tmp/backup-restore-test.sql");

        Assert.Equal("/usr/bin/psql", runner.CapturedFileName);
        Assert.NotNull(runner.CapturedArgs);
        Assert.Contains("-v", runner.CapturedArgs!);
        Assert.Contains("ON_ERROR_STOP=1", runner.CapturedArgs!);
        Assert.Contains("-f", runner.CapturedArgs!);
        Assert.Contains("/tmp/backup-restore-test.sql", runner.CapturedArgs!);
    }

    /// <summary>
    /// maliciousDumpPath is crafted to look like a subprocess argument-injection
    /// payload (extra flags via ';'/'--', shell metacharacters) and must
    /// survive as one literal argument-vector element passed to psql's -f
    /// flag, never reinterpreted/split — arguments go through
    /// ProcessStartInfo.ArgumentList (never a concatenated shell command
    /// string), matching PgDumpBackupService's existing defense.
    /// </summary>
    [Theory]
    [InlineData("/tmp/evil;rm -rf /.sql")]
    [InlineData("/tmp/evil --set=malicious.sql")]
    [InlineData("/tmp/evil`whoami`.sql")]
    public async Task RestoreAsync_DumpFilePathInjectionAttempt_PassedAsLiteralArgVectorElement(string maliciousDumpPath)
    {
        FakeProcessRunner runner = new() { ResultToReturn = new ProcessResult(0, string.Empty, string.Empty) };
        IConfiguration configuration = BuildConfiguration(
            "Host=localhost;Port=5432;Database=club12;Username=app;Password=x");
        PsqlDatabaseRestoreService service = new(runner, configuration, NullLogger<PsqlDatabaseRestoreService>.Instance);

        await service.RestoreAsync(maliciousDumpPath);

        Assert.NotNull(runner.CapturedArgs);
        Assert.Single(runner.CapturedArgs!, a => a == maliciousDumpPath);
        Assert.Equal(1, runner.CallCount);
    }

    [Fact]
    public async Task RestoreAsync_NonZeroExitCode_ThrowsBackupExecutionException_NotUncaught()
    {
        FakeProcessRunner runner = new()
        {
            ResultToReturn = new ProcessResult(1, string.Empty, "psql: error: syntax error at or near \"BOGUS\""),
        };
        IConfiguration configuration = BuildConfiguration(
            "Host=localhost;Port=5432;Database=club12;Username=app;Password=x");
        PsqlDatabaseRestoreService service = new(runner, configuration, NullLogger<PsqlDatabaseRestoreService>.Instance);

        BackupExecutionException ex = await Assert.ThrowsAsync<BackupExecutionException>(
            () => service.RestoreAsync("/tmp/backup-restore-test.sql"));

        Assert.Contains("exit code 1", ex.Message);
        Assert.Contains("syntax error", ex.Message);
    }

    /// <summary>
    /// Simulates what ProcessRunner returns when Process.Start fails for a
    /// missing executable: sentinel exit code -1, detail in StdErr.
    /// </summary>
    [Fact]
    public async Task RestoreAsync_MissingBinary_ThrowsHandledExceptionWithActionableMessage()
    {
        FakeProcessRunner runner = new()
        {
            ResultToReturn = new ProcessResult(
                -1, string.Empty, "Failed to start process 'psql': The system cannot find the file specified."),
        };
        IConfiguration configuration = BuildConfiguration(
            "Host=localhost;Port=5432;Database=club12;Username=app;Password=x", psqlPath: "psql");
        PsqlDatabaseRestoreService service = new(runner, configuration, NullLogger<PsqlDatabaseRestoreService>.Instance);

        BackupExecutionException ex = await Assert.ThrowsAsync<BackupExecutionException>(
            () => service.RestoreAsync("/tmp/backup-restore-test.sql"));

        Assert.Contains("psql", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("PATH", ex.Message);
    }

    [Fact]
    public async Task RestoreAsync_MissingConnectionString_ThrowsBackupExecutionException()
    {
        FakeProcessRunner runner = new();
        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection([]).Build();
        PsqlDatabaseRestoreService service = new(runner, configuration, NullLogger<PsqlDatabaseRestoreService>.Instance);

        await Assert.ThrowsAsync<BackupExecutionException>(
            () => service.RestoreAsync("/tmp/backup-restore-test.sql"));

        Assert.Equal(0, runner.CallCount);
    }
}
