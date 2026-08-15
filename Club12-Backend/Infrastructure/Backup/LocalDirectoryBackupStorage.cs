using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Application.Interfaces.Backup;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Backup;

/// <summary>
/// IBackupStorage implementation backed by a local/mounted
/// directory — the config-selected fallback for environments without
/// Supabase storage configured (or the default until the Supabase adapter
/// ships in a later work unit). Backup names are expected to be
/// server-generated (timestamp + guid, see
/// DatabaseBackupHostedService), but every name is still validated to
/// resolve strictly inside the configured directory before any file
/// operation runs, defending against path traversal regardless of caller.
/// </summary>
public sealed class LocalDirectoryBackupStorage : IBackupStorage
{
    private readonly string _directoryPath;
    private readonly ILogger<LocalDirectoryBackupStorage> _logger;

    public LocalDirectoryBackupStorage(string directoryPath, ILogger<LocalDirectoryBackupStorage> logger)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(directoryPath);
        _logger = logger;
        _directoryPath = Path.GetFullPath(directoryPath);
        Directory.CreateDirectory(_directoryPath);
    }

    public async Task StoreAsync(string name, Stream content, CancellationToken ct = default)
    {
        string path = ResolveSafePath(name);
        await using FileStream file = File.Create(path);
        await content.CopyToAsync(file, ct);
    }

    public Task<IReadOnlyList<BackupFile>> ListAsync(CancellationToken ct = default)
    {
        IReadOnlyList<BackupFile> files = Directory.EnumerateFiles(_directoryPath)
            .Select(path => new BackupFile(
                Path.GetFileName(path),
                new DateTimeOffset(File.GetLastWriteTimeUtc(path), TimeSpan.Zero)))
            .ToList();

        return Task.FromResult(files);
    }

    public Task DeleteAsync(string name, CancellationToken ct = default)
    {
        string path = ResolveSafePath(name);
        if (File.Exists(path))
            File.Delete(path);
        else
            _logger.LogWarning("DeleteAsync called for a backup that no longer exists: {Name}", name);

        return Task.CompletedTask;
    }

    /// <summary>
    /// Resolves <paramref name="name"/> against the configured directory and
    /// throws ArgumentException unless the resolved path stays
    /// strictly inside it (defends against ../ traversal and rooted
    /// absolute paths escaping the configured backup directory).
    /// </summary>
    private string ResolveSafePath(string name)
    {
        if (string.IsNullOrWhiteSpace(name) || Path.IsPathRooted(name))
            throw new ArgumentException("Backup file name must be a non-empty, relative name.", nameof(name));

        string fullPath = Path.GetFullPath(Path.Combine(_directoryPath, name));
        string prefix = _directoryPath.EndsWith(Path.DirectorySeparatorChar)
            ? _directoryPath
            : _directoryPath + Path.DirectorySeparatorChar;

        if (!fullPath.StartsWith(prefix, StringComparison.Ordinal))
            throw new ArgumentException(
                $"Backup file name '{name}' resolves outside the configured backup directory.", nameof(name));

        return fullPath;
    }
}
