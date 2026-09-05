using Application.Interfaces.Backup;
using Application.Utils.Helper.SupabaseHelper;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Infrastructure.Backup;

/// <summary>
/// IBackupStorage implementation that reuses the existing Supabase client and credentials through ISupabaseRawStorage instead of provisioning a second Supabase client.
/// </summary>
public sealed class SupabaseBackupStorage(ISupabaseRawStorage rawStorage) : IBackupStorage
{
    private const string Prefix = "backups/";

    public async Task StoreAsync(string name, Stream content, CancellationToken ct = default)
    {
        string objectPath = ToObjectPath(name);

        try
        {
            await rawStorage.UploadRawAsync(objectPath, content);
        }
        catch (Exception ex)
        {
            throw new BackupExecutionException(
                $"Failed to upload backup '{name}' to Supabase storage: {ex.Message}", ex);
        }
    }

    public async Task<Stream> OpenReadAsync(string name, CancellationToken ct = default)
    {
        string objectPath = ToObjectPath(name);

        try
        {
            byte[] bytes = await rawStorage.DownloadRawAsync(objectPath);
            return new MemoryStream(bytes);
        }
        catch (Exception ex)
        {
            throw new BackupExecutionException(
                $"Failed to download backup '{name}' from Supabase storage: {ex.Message}", ex);
        }
    }

    public async Task<IReadOnlyList<BackupFile>> ListAsync(CancellationToken ct = default)
    {
        try
        {
            IReadOnlyList<SupabaseStorageEntry> entries = await rawStorage.ListRawAsync(Prefix);
            return entries
                .Where(entry => !string.IsNullOrEmpty(entry.Name))
                .Select(entry => new BackupFile(entry.Name, entry.UpdatedAt ?? DateTimeOffset.MinValue))
                .ToList();
        }
        catch (Exception ex)
        {
            throw new BackupExecutionException($"Failed to list Supabase backups: {ex.Message}", ex);
        }
    }

    public async Task DeleteAsync(string name, CancellationToken ct = default)
    {
        string objectPath = ToObjectPath(name);

        try
        {
            await rawStorage.RemoveRawAsync(objectPath);
        }
        catch (Exception ex)
        {
            throw new BackupExecutionException(
                $"Failed to delete backup '{name}' from Supabase storage: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Resolves name against the backups/ prefix and throws ArgumentException unless it is a simple, non-rooted, non-traversing relative name.
    /// </summary>
    private static string ToObjectPath(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Backup file name must be non-empty.", nameof(name));
        }

        if (Path.IsPathRooted(name))
        {
            throw new ArgumentException(
                $"Backup file name '{name}' must be a relative name, not a rooted path.", nameof(name));
        }

        string normalized = name.Replace('\\', '/');
        string[] segments = normalized.Split('/', StringSplitOptions.RemoveEmptyEntries);
        return segments.Length == 0 || Array.Exists(segments, segment => segment is ".." or ".")
            ? throw new ArgumentException(
                $"Backup file name '{name}' resolves outside the configured '{Prefix}' prefix.", nameof(name))
            : Prefix + normalized;
    }
}
