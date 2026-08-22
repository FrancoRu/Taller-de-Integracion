using API.Tests.Backup.Fakes;

using Application.Interfaces.Backup;

using Infrastructure.Backup;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace API.Tests.Backup;

/// <summary>
/// Unit tests for PgDumpBackupService using a
/// FakeProcessRunner — no real pg_dump binary involved.
/// Covers: correct argument-vector construction from the connection string,
/// subprocess argument-injection resistance, and failure handling
/// (non-zero exit / missing binary → logged, handled exception, not a crash).
/// </summary>
public class PgDumpBackupServiceTests
{
    private static IConfiguration BuildConfiguration(string connectionString, string? pgDumpPath = null)
    {
        Dictionary<string, string?> values = new()
        {
            ["ConnectionStrings:DbConnection"] = connectionString,
        };
        if (pgDumpPath is not null)
        {
            values["Backup:PgDumpPath"] = pgDumpPath;
        }

        return new ConfigurationBuilder().AddInMemoryCollection(values).Build();
    }

    [Fact]
    public async Task CreateDumpAsync_SuccessfulExit_ReturnsStdOutAsStream()
    {
        FakeProcessRunner runner = new()
        {
            ResultToReturn = new ProcessResult(0, "-- pg_dump output --", string.Empty),
        };
        IConfiguration configuration = BuildConfiguration(
            "Host=localhost;Port=5432;Database=club12;Username=app;Password=secret");
        PgDumpBackupService service = new(runner, configuration, NullLogger<PgDumpBackupService>.Instance);

        await using Stream dump = await service.CreateDumpAsync();

        using StreamReader reader = new(dump);
        string content = await reader.ReadToEndAsync();
        Assert.Equal("-- pg_dump output --", content);
    }

    /// <summary>
    /// The password must never appear in the argument vector, since it would
    /// otherwise leak via `ps`/task list; it is instead passed through the
    /// PGPASSWORD environment variable.
    /// </summary>
    [Fact]
    public async Task CreateDumpAsync_BuildsArgumentVectorFromConnectionString_PasswordNeverInArgs()
    {
        FakeProcessRunner runner = new() { ResultToReturn = new ProcessResult(0, "ok", string.Empty) };
        IConfiguration configuration = BuildConfiguration(
            "Host=db.internal;Port=5433;Database=club12;Username=app_user;Password=s3cret");

        PgDumpBackupService service = new(runner, configuration, NullLogger<PgDumpBackupService>.Instance);

        await service.CreateDumpAsync();

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
    /// maliciousDbName is crafted to look like a shell-injection payload and
    /// must survive as one literal argument-vector element, never concatenated
    /// into a shell command string that a shell could re-tokenize/expand. ';'
    /// and '=' are reserved separators in the ADO connection-string format
    /// itself — a different boundary than the subprocess argument vector under
    /// test here — so the payload avoids those two characters and instead uses
    /// shell metacharacters: $(), backticks, pipes, and &.
    /// </summary>
    [Fact]
    public async Task CreateDumpAsync_ArgumentInjectionAttempt_PassedAsLiteralArgVectorElement()
    {
        FakeProcessRunner runner = new() { ResultToReturn = new ProcessResult(0, "ok", string.Empty) };
        const string maliciousDbName = "club12$(touch pwned)`whoami`|evil&";
        IConfiguration configuration = BuildConfiguration(
            $"Host=localhost;Port=5432;Database={maliciousDbName};Username=app;Password=x");

        PgDumpBackupService service = new(runner, configuration, NullLogger<PgDumpBackupService>.Instance);

        await service.CreateDumpAsync();

        Assert.Contains(maliciousDbName, runner.CapturedArgs!);
        Assert.All(runner.CapturedArgs!, a => Assert.DoesNotContain("&&", a));
    }

    /// <summary>
    /// Restoring with psql -f against a Supabase-managed database can
    /// fail on ownership/role statements captured by a plain pg_dump;
    /// these flags move that safety onto the dump side (design.md's
    /// "Keep plain-SQL dumps; restore with psql" decision).
    /// </summary>
    [Fact]
    public async Task CreateDumpAsync_IncludesCleanAndOwnershipSafetyFlags()
    {
        FakeProcessRunner runner = new() { ResultToReturn = new ProcessResult(0, "ok", string.Empty) };
        IConfiguration configuration = BuildConfiguration(
            "Host=localhost;Port=5432;Database=club12;Username=app;Password=x");
        PgDumpBackupService service = new(runner, configuration, NullLogger<PgDumpBackupService>.Instance);

        await service.CreateDumpAsync();

        Assert.NotNull(runner.CapturedArgs);
        Assert.Contains("--clean", runner.CapturedArgs!);
        Assert.Contains("--if-exists", runner.CapturedArgs!);
        Assert.Contains("--no-owner", runner.CapturedArgs!);
        Assert.Contains("--no-privileges", runner.CapturedArgs!);
    }

    /// <summary>
    /// Supabase-managed databases carry platform-internal event triggers
    /// (PostgREST's schema-cache-reload hooks, pgsodium's mask-update hook,
    /// etc.) that a plain pg_dump captures because event triggers are
    /// database-wide, not schema-scoped — no `-n`/`--exclude-schema` filters
    /// them out. On restore, `--clean`'s `DROP EVENT TRIGGER` for one of
    /// these fails with "must be owner of event trigger", since the app's
    /// connection role never owns Supabase's own infrastructure objects. The
    /// app never defines its own event triggers, so any EVENT TRIGGER
    /// statement in the dump is guaranteed to be Supabase-owned and safe to
    /// drop from the dump entirely (not just the one named in the incident —
    /// psql aborts at the first one, so others further down the dump would
    /// never even surface).
    /// </summary>
    [Fact]
    public async Task CreateDumpAsync_StripsEventTriggerStatements_SupabaseInternalObjectsNotOwnedByAppRole()
    {
        const string rawDump = """
            SET statement_timeout = 0;
            DROP EVENT TRIGGER IF EXISTS pgrst_drop_watch;
            DROP EVENT TRIGGER IF EXISTS pgrst_ddl_watch;
            CREATE TABLE "Club12"."BackupRecords" (
                "Id" uuid NOT NULL
            );
            CREATE EVENT TRIGGER pgrst_drop_watch ON sql_drop
               EXECUTE FUNCTION extensions.pgrst_drop_watch();
            CREATE EVENT TRIGGER pgrst_ddl_watch ON ddl_command_end
               EXECUTE FUNCTION extensions.pgrst_ddl_watch();
            COMMENT ON EVENT TRIGGER pgrst_drop_watch IS 'notify PostgREST of DDL changes';
            INSERT INTO "Club12"."BackupRecords" ("Id") VALUES ('11111111-1111-1111-1111-111111111111');
            """;
        FakeProcessRunner runner = new() { ResultToReturn = new ProcessResult(0, rawDump, string.Empty) };
        IConfiguration configuration = BuildConfiguration(
            "Host=localhost;Port=5432;Database=club12;Username=app;Password=x");
        PgDumpBackupService service = new(runner, configuration, NullLogger<PgDumpBackupService>.Instance);

        await using Stream dump = await service.CreateDumpAsync();

        using StreamReader reader = new(dump);
        string content = await reader.ReadToEndAsync();
        Assert.DoesNotContain("EVENT TRIGGER", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("CREATE TABLE \"Club12\".\"BackupRecords\"", content);
        Assert.Contains("INSERT INTO \"Club12\".\"BackupRecords\"", content);
    }

    [Fact]
    public async Task CreateDumpAsync_NonZeroExitCode_ThrowsBackupExecutionException_NotUncaught()
    {
        FakeProcessRunner runner = new()
        {
            ResultToReturn = new ProcessResult(1, string.Empty, "pg_dump: error: connection failed"),
        };
        IConfiguration configuration = BuildConfiguration(
            "Host=localhost;Port=5432;Database=club12;Username=app;Password=x");
        PgDumpBackupService service = new(runner, configuration, NullLogger<PgDumpBackupService>.Instance);

        BackupExecutionException ex = await Assert.ThrowsAsync<BackupExecutionException>(
            () => service.CreateDumpAsync());

        Assert.Contains("exit code 1", ex.Message);
        Assert.Contains("connection failed", ex.Message);
    }

    /// <summary>
    /// Simulates what ProcessRunner returns when Process.Start fails for a
    /// missing executable: sentinel exit code -1, detail in StdErr.
    /// </summary>
    [Fact]
    public async Task CreateDumpAsync_MissingBinary_ThrowsHandledExceptionWithActionableMessage()
    {
        FakeProcessRunner runner = new()
        {
            ResultToReturn = new ProcessResult(
                -1, string.Empty, "Failed to start process 'pg_dump': The system cannot find the file specified."),
        };
        IConfiguration configuration = BuildConfiguration(
            "Host=localhost;Port=5432;Database=club12;Username=app;Password=x",
            pgDumpPath: "pg_dump");
        PgDumpBackupService service = new(runner, configuration, NullLogger<PgDumpBackupService>.Instance);

        BackupExecutionException ex = await Assert.ThrowsAsync<BackupExecutionException>(
            () => service.CreateDumpAsync());

        Assert.Contains("pg_dump", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("PATH", ex.Message);
    }

    [Fact]
    public async Task CreateDumpAsync_MissingConnectionString_ThrowsBackupExecutionException()
    {
        FakeProcessRunner runner = new();
        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(
            []).Build();
        PgDumpBackupService service = new(runner, configuration, NullLogger<PgDumpBackupService>.Instance);

        await Assert.ThrowsAsync<BackupExecutionException>(() => service.CreateDumpAsync());

        Assert.Equal(0, runner.CallCount);
    }
}
