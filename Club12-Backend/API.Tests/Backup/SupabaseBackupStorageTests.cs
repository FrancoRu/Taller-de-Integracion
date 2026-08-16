using API.Tests.Backup.Fakes;

using Application.Interfaces.Backup;

using Infrastructure.Backup;

using System.Text;

namespace API.Tests.Backup;

/// <summary>
/// Unit tests for SupabaseBackupStorage against a
/// FakeSupabaseRawStorage — no real Supabase client, no
/// network call (per this change's spec Non-Goal: actual Supabase upload is
/// staging/manual verification only). Covers: the backups/
/// object-path builder, traversal rejection, list translation, and wrapping
/// of raw-storage failures (network/auth errors) into
/// BackupExecutionException — the same failure type
/// PgDumpBackupService throws and DatabaseBackupHostedService
/// already catches and logs without crashing the host.
/// </summary>
public sealed class SupabaseBackupStorageTests
{
    private static Stream ContentStream(string text)
    {
        return new MemoryStream(Encoding.UTF8.GetBytes(text));
    }

    [Fact]
    public async Task StoreAsync_ValidName_UploadsUnderBackupsPrefix()
    {
        FakeSupabaseRawStorage raw = new();
        SupabaseBackupStorage storage = new(raw);

        await storage.StoreAsync("backup-1.sql", ContentStream("dump"));

        Assert.Single(raw.UploadedPaths);
        Assert.Equal("backups/backup-1.sql", raw.UploadedPaths[0]);
    }

    [Theory]
    [InlineData("../escape.sql")]
    [InlineData("../../escape.sql")]
    [InlineData("nested/../../escape.sql")]
    public async Task StoreAsync_RelativeTraversalName_ThrowsArgumentException_NoUploadAttempted(string maliciousName)
    {
        FakeSupabaseRawStorage raw = new();
        SupabaseBackupStorage storage = new(raw);

        await Assert.ThrowsAsync<ArgumentException>(() => storage.StoreAsync(maliciousName, ContentStream("evil")));

        Assert.Empty(raw.UploadedPaths);
    }

    [Fact]
    public async Task StoreAsync_RootedPathName_ThrowsArgumentException_NoUploadAttempted()
    {
        FakeSupabaseRawStorage raw = new();
        SupabaseBackupStorage storage = new(raw);
        string rooted = OperatingSystem.IsWindows() ? "C:\\evil.sql" : "/etc/evil.sql";

        await Assert.ThrowsAsync<ArgumentException>(() => storage.StoreAsync(rooted, ContentStream("evil")));

        Assert.Empty(raw.UploadedPaths);
    }

    [Fact]
    public async Task DeleteAsync_ValidName_RemovesUnderBackupsPrefix()
    {
        FakeSupabaseRawStorage raw = new();
        SupabaseBackupStorage storage = new(raw);

        await storage.DeleteAsync("backup-1.sql");

        Assert.Single(raw.RemovedPaths);
        Assert.Equal("backups/backup-1.sql", raw.RemovedPaths[0]);
    }

    [Fact]
    public async Task DeleteAsync_TraversalName_ThrowsArgumentException_NoRemoveAttempted()
    {
        FakeSupabaseRawStorage raw = new();
        SupabaseBackupStorage storage = new(raw);

        await Assert.ThrowsAsync<ArgumentException>(() => storage.DeleteAsync("../escape.sql"));

        Assert.Empty(raw.RemovedPaths);
    }

    [Fact]
    public async Task ListAsync_TranslatesRawEntries_UsingBackupsPrefix()
    {
        DateTimeOffset updated = DateTimeOffset.UtcNow.AddMinutes(-5);
        FakeSupabaseRawStorage raw = new()
        {
            EntriesToList =
            [
                new("backup-1.sql", updated),
                new("backup-2.sql", null),
            ],
        };
        SupabaseBackupStorage storage = new(raw);

        IReadOnlyList<BackupFile> files = await storage.ListAsync();

        Assert.Equal("backups/", raw.LastListedPrefix);
        Assert.Equal(2, files.Count);
        Assert.Contains(files, f => f.Name == "backup-1.sql" && f.Timestamp == updated);
        Assert.Contains(files, f => f.Name == "backup-2.sql");
    }

    [Fact]
    public async Task StoreAsync_RawStorageThrowsNetworkError_WrapsIntoBackupExecutionException()
    {
        FakeSupabaseRawStorage raw = new() { ExceptionToThrow = new HttpRequestException("network unreachable") };
        SupabaseBackupStorage storage = new(raw);

        BackupExecutionException ex = await Assert.ThrowsAsync<BackupExecutionException>(
            () => storage.StoreAsync("backup-1.sql", ContentStream("dump")));

        Assert.Contains("backup-1.sql", ex.Message);
        Assert.IsType<HttpRequestException>(ex.InnerException);
    }

    [Fact]
    public async Task ListAsync_RawStorageThrowsAuthError_WrapsIntoBackupExecutionException()
    {
        FakeSupabaseRawStorage raw = new() { ExceptionToThrow = new InvalidOperationException("401 unauthorized") };
        SupabaseBackupStorage storage = new(raw);

        BackupExecutionException ex = await Assert.ThrowsAsync<BackupExecutionException>(() => storage.ListAsync());

        Assert.Contains("401 unauthorized", ex.Message);
    }

    [Fact]
    public async Task DeleteAsync_RawStorageThrows_WrapsIntoBackupExecutionException()
    {
        FakeSupabaseRawStorage raw = new() { ExceptionToThrow = new InvalidOperationException("boom") };
        SupabaseBackupStorage storage = new(raw);

        await Assert.ThrowsAsync<BackupExecutionException>(() => storage.DeleteAsync("backup-1.sql"));
    }
}
