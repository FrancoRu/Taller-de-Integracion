using Application.Utils.Helper.SupabaseHelper;

namespace API.Tests.Backup.Fakes;

/// <summary>
/// Test double for ISupabaseRawStorage. Records the exact
/// object paths (and, per HU medical-records-storage-eligibility, the target
/// bucket) passed to each raw call and can be configured to throw (simulating
/// a network/auth error from the real Supabase client), so
/// SupabaseBackupStorage and SupabaseMedicalRecordStorage can be unit tested
/// without a real SupabaseHelper (whose constructor performs real network
/// initialization) or any network call — per this change's spec Non-Goal on
/// actual Supabase upload verification.
/// </summary>
public sealed class FakeSupabaseRawStorage : ISupabaseRawStorage
{
    public List<string> UploadedPaths { get; } = [];

    /// <summary>
    /// The <c>bucket</c> argument passed to each <see cref="UploadRawAsync"/>
    /// call, in order — <see langword="null"/> when the caller did not pass a
    /// bucket (i.e. relied on the configured default).
    /// </summary>
    public List<string?> UploadedBuckets { get; } = [];

    public List<string> RemovedPaths { get; } = [];
    public string? LastListedPrefix { get; private set; }
    public string? LastDownloadedPath { get; private set; }

    /// <summary>The <c>bucket</c> argument passed to the last <see cref="DownloadRawAsync"/> call.</summary>
    public string? DownloadedBucket { get; private set; }

    public IReadOnlyList<SupabaseStorageEntry> EntriesToList { get; set; } = Array.Empty<SupabaseStorageEntry>();

    public byte[] BytesToDownload { get; set; } = Array.Empty<byte>();

    /// <summary>
    /// When set, every raw call throws this exception instead of succeeding —
    /// simulates a network error, auth error, or any other Supabase client
    /// failure.
    /// </summary>
    public Exception? ExceptionToThrow { get; set; }

    public Task UploadRawAsync(string objectPath, Stream content, string? bucket = null)
    {
        if (ExceptionToThrow is not null)
        {
            throw ExceptionToThrow;
        }

        UploadedPaths.Add(objectPath);
        UploadedBuckets.Add(bucket);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<SupabaseStorageEntry>> ListRawAsync(string prefix, string? bucket = null)
    {
        if (ExceptionToThrow is not null)
        {
            throw ExceptionToThrow;
        }

        LastListedPrefix = prefix;
        return Task.FromResult(EntriesToList);
    }

    public Task RemoveRawAsync(string objectPath, string? bucket = null)
    {
        if (ExceptionToThrow is not null)
        {
            throw ExceptionToThrow;
        }

        RemovedPaths.Add(objectPath);
        return Task.CompletedTask;
    }

    public Task<byte[]> DownloadRawAsync(string objectPath, string? bucket = null)
    {
        if (ExceptionToThrow is not null)
        {
            throw ExceptionToThrow;
        }

        LastDownloadedPath = objectPath;
        DownloadedBucket = bucket;
        return Task.FromResult(BytesToDownload);
    }
}
