using API.Tests.Backup.Fakes;

using Application.Interfaces.Backup;

using Infrastructure.Backup;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

using System.Text;

namespace API.Tests.Backup;

/// <summary>
/// Unit tests for PgRestoreService using a FakeProcessRunner — no real psql
/// binary involved. Covers argument-vector construction from the connection
/// string (password via PGPASSWORD, never in args), the transient
/// restore-copy temp file being cleaned up, and failure handling (non-zero
/// exit / missing binary => handled BackupExecutionException, not a crash).
/// </summary>
public class PgRestoreServiceTests
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

    private static Stream DumpStream(string sql = "SELECT 1;")
    {
        return new MemoryStream(Encoding.UTF8.GetBytes(sql));
    }

    [Fact]
    public async Task RestoreAsync_BuildsArgumentVector_PasswordViaEnv_NotInArgs()
    {
        FakeProcessRunner runner = new() { ResultToReturn = new ProcessResult(0, string.Empty, string.Empty) };
        IConfiguration configuration = BuildConfiguration(
            "Host=db.internal;Port=5433;Database=club12;Username=app_user;Password=s3cret");
        PgRestoreService service = new(runner, configuration, NullLogger<PgRestoreService>.Instance);

        await service.RestoreAsync(DumpStream());

        Assert.NotNull(runner.CapturedArgs);
        Assert.Contains("db.internal", runner.CapturedArgs!);
        Assert.Contains("5433", runner.CapturedArgs!);
        Assert.Contains("app_user", runner.CapturedArgs!);
        Assert.Contains("club12", runner.CapturedArgs!);
        Assert.Contains("ON_ERROR_STOP=1", runner.CapturedArgs!);
        Assert.Contains("-f", runner.CapturedArgs!);
        Assert.DoesNotContain("s3cret", runner.CapturedArgs!);
        Assert.Equal("s3cret", runner.CapturedEnvironmentVariables!["PGPASSWORD"]);
    }

    [Fact]
    public async Task RestoreAsync_Success_DeletesTransientRestoreCopy()
    {
        FakeProcessRunner runner = new() { ResultToReturn = new ProcessResult(0, string.Empty, string.Empty) };
        IConfiguration configuration = BuildConfiguration(
            "Host=localhost;Port=5432;Database=club12;Username=app;Password=x");
        PgRestoreService service = new(runner, configuration, NullLogger<PgRestoreService>.Instance);

        await service.RestoreAsync(DumpStream());

        int fileArgIndex = ((List<string>) runner.CapturedArgs!).IndexOf("-f");
        string restoreCopyPath = runner.CapturedArgs![fileArgIndex + 1];
        Assert.False(File.Exists(restoreCopyPath), "The transient restore copy must be deleted after restore.");
    }

    [Fact]
    public async Task RestoreAsync_NonZeroExit_ThrowsBackupExecutionException()
    {
        FakeProcessRunner runner = new()
        {
            ResultToReturn = new ProcessResult(1, string.Empty, "psql: error: relation does not exist"),
        };
        IConfiguration configuration = BuildConfiguration(
            "Host=localhost;Port=5432;Database=club12;Username=app;Password=x");
        PgRestoreService service = new(runner, configuration, NullLogger<PgRestoreService>.Instance);

        BackupExecutionException ex = await Assert.ThrowsAsync<BackupExecutionException>(
            () => service.RestoreAsync(DumpStream()));

        Assert.Contains("exit code 1", ex.Message);
        Assert.Contains("does not exist", ex.Message);
    }

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
        PgRestoreService service = new(runner, configuration, NullLogger<PgRestoreService>.Instance);

        BackupExecutionException ex = await Assert.ThrowsAsync<BackupExecutionException>(
            () => service.RestoreAsync(DumpStream()));

        Assert.Contains("psql", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("PATH", ex.Message);
    }

    [Fact]
    public async Task RestoreAsync_MissingConnectionString_Throws_NeverRunsProcess()
    {
        FakeProcessRunner runner = new();
        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection([]).Build();
        PgRestoreService service = new(runner, configuration, NullLogger<PgRestoreService>.Instance);

        await Assert.ThrowsAsync<BackupExecutionException>(() => service.RestoreAsync(DumpStream()));
        Assert.Equal(0, runner.CallCount);
    }
}
