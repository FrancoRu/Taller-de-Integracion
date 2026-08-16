using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using API.Tests.Backup.Fakes;
using Application.Interfaces.Backup;
using Infrastructure.Backup;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

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
            values["Backup:PgDumpPath"] = pgDumpPath;

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
            new Dictionary<string, string?>()).Build();
        PgDumpBackupService service = new(runner, configuration, NullLogger<PgDumpBackupService>.Instance);

        await Assert.ThrowsAsync<BackupExecutionException>(() => service.CreateDumpAsync());

        Assert.Equal(0, runner.CallCount);
    }
}
