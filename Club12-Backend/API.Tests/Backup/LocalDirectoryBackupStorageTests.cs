using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Application.Backup;
using Application.Interfaces.Backup;
using Infrastructure.Backup;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace API.Tests.Backup;

/// <summary>
/// Tests LocalDirectoryBackupStorage against a real temporary
/// directory (safe: local filesystem only, no external dependency), plus an
/// end-to-end integration with the real KeepLastNRetentionPolicy
/// to prove retention actually deletes the correct files when invoked
/// through the storage adapter.
/// </summary>
public sealed class LocalDirectoryBackupStorageTests : IDisposable
{
    private readonly string _tempDir;
    private readonly LocalDirectoryBackupStorage _storage;

    public LocalDirectoryBackupStorageTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "club12-backup-tests-" + Guid.NewGuid().ToString("N"));
        _storage = new LocalDirectoryBackupStorage(_tempDir, NullLogger<LocalDirectoryBackupStorage>.Instance);
    }

    private static Stream ContentStream(string text) => new MemoryStream(Encoding.UTF8.GetBytes(text));

    [Fact]
    public async Task StoreAsync_ThenListAsync_ReturnsStoredFile()
    {
        await _storage.StoreAsync("backup-1.sql", ContentStream("dump-1"));

        IReadOnlyList<BackupFile> files = await _storage.ListAsync();

        Assert.Single(files);
        Assert.Equal("backup-1.sql", files[0].Name);
        Assert.Equal("dump-1", await File.ReadAllTextAsync(Path.Combine(_tempDir, "backup-1.sql")));
    }

    [Fact]
    public async Task DeleteAsync_RemovesFile_ListNoLongerContainsIt()
    {
        await _storage.StoreAsync("backup-1.sql", ContentStream("dump-1"));
        await _storage.StoreAsync("backup-2.sql", ContentStream("dump-2"));

        await _storage.DeleteAsync("backup-1.sql");
        IReadOnlyList<BackupFile> files = await _storage.ListAsync();

        Assert.Single(files);
        Assert.Equal("backup-2.sql", files[0].Name);
        Assert.False(File.Exists(Path.Combine(_tempDir, "backup-1.sql")));
    }

    [Fact]
    public async Task StoreAsync_NameEscapingConfiguredDirectory_ThrowsArgumentException()
    {
        await Assert.ThrowsAsync<ArgumentException>(
            () => _storage.StoreAsync("../escape.sql", ContentStream("evil")));
    }

    [Fact]
    public async Task StoreAsync_RootedPathName_ThrowsArgumentException()
    {
        string rooted = OperatingSystem.IsWindows() ? "C:\\evil.sql" : "/etc/evil.sql";

        await Assert.ThrowsAsync<ArgumentException>(
            () => _storage.StoreAsync(rooted, ContentStream("evil")));
    }

    [Fact]
    public async Task DeleteAsync_NameEscapingConfiguredDirectory_ThrowsArgumentException()
    {
        await Assert.ThrowsAsync<ArgumentException>(() => _storage.DeleteAsync("../../escape.sql"));
    }

    [Fact]
    public async Task RetentionIntegration_StoresNPlus2_RetainsNewestN_DeletesExactlyTwoPerTieBreak()
    {
        // retainCount = 2; 4 files stored (N+2). One tied pair straddles the
        // retention boundary to exercise unit 1's documented tie-break rule
        // (newest timestamp first, then lexically-smallest name retained).
        const int retainCount = 2;
        DateTime tied = DateTime.UtcNow.AddMinutes(-10);

        await StoreWithTimestamp("file-newest.sql", DateTime.UtcNow.AddMinutes(-5));
        await StoreWithTimestamp("file-tied-a.sql", tied);
        await StoreWithTimestamp("file-tied-b.sql", tied);
        await StoreWithTimestamp("file-oldest.sql", DateTime.UtcNow.AddMinutes(-40));

        IReadOnlyList<BackupFile> existing = await _storage.ListAsync();
        Assert.Equal(4, existing.Count);

        KeepLastNRetentionPolicy retention = new();
        IReadOnlyList<BackupFile> toDelete = retention.SelectForDeletion(existing, retainCount);

        Assert.Equal(2, toDelete.Count);
        Assert.Equal(
            new[] { "file-oldest.sql", "file-tied-b.sql" },
            toDelete.Select(f => f.Name).OrderBy(n => n, StringComparer.Ordinal).ToArray());

        foreach (BackupFile stale in toDelete)
            await _storage.DeleteAsync(stale.Name);

        IReadOnlyList<BackupFile> remaining = await _storage.ListAsync();
        Assert.Equal(2, remaining.Count);
        Assert.Contains(remaining, f => f.Name == "file-newest.sql");
        Assert.Contains(remaining, f => f.Name == "file-tied-a.sql");
    }

    private async Task StoreWithTimestamp(string name, DateTime timestampUtc)
    {
        await _storage.StoreAsync(name, ContentStream("dump-content-for-" + name));
        File.SetLastWriteTimeUtc(Path.Combine(_tempDir, name), timestampUtc);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }
}
