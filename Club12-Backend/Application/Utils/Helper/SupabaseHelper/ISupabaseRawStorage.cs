using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Application.Utils.Helper.SupabaseHelper;

/// <summary>
/// Narrow raw-storage boundary over the Supabase Storage client, implemented
/// by SupabaseHelper. Unlike SupabaseHelper.UploadImageAsync{T}
/// and SupabaseHelper.DeleteImageAsync{T}, these methods are not
/// image-shaped (no per-type folder convention, no public-URL return) — the
/// caller supplies the full object path. Exists so
/// SupabaseBackupStorage can be unit-tested
/// against a fake without constructing a real SupabaseHelper
/// (whose constructor performs real network initialization) or touching the
/// network — actual Supabase upload/list/remove behavior is this change's
/// spec Non-Goal for automated tests and requires staging/manual
/// verification instead.
/// </summary>
public interface ISupabaseRawStorage
{
    /// <summary>
    /// Uploads raw content to <paramref name="objectPath"/> in <paramref name="bucket"/>.
    /// </summary>
    /// <param name="objectPath">The exact destination path within the bucket.</param>
    /// <param name="content">The content stream to upload.</param>
    /// <param name="bucket">Target bucket; null uses the configured SupaBase:BucketName.</param>
    Task UploadRawAsync(string objectPath, Stream content, string? bucket = null);

    /// <summary>
    /// Lists raw objects under <paramref name="prefix"/> in <paramref name="bucket"/>.
    /// </summary>
    /// <param name="prefix">The bucket-relative path prefix to list.</param>
    /// <param name="bucket">Target bucket; null uses the configured SupaBase:BucketName.</param>
    Task<IReadOnlyList<SupabaseStorageEntry>> ListRawAsync(string prefix, string? bucket = null);

    /// <summary>
    /// Removes the raw object at <paramref name="objectPath"/> in <paramref name="bucket"/>.
    /// </summary>
    /// <param name="objectPath">The exact object path within the bucket.</param>
    /// <param name="bucket">Target bucket; null uses the configured SupaBase:BucketName.</param>
    Task RemoveRawAsync(string objectPath, string? bucket = null);

    /// <summary>
    /// Downloads the raw content of the object at <paramref name="objectPath"/>
    /// in <paramref name="bucket"/>.
    /// </summary>
    /// <param name="objectPath">The exact object path within the bucket.</param>
    /// <param name="bucket">Target bucket; null uses the configured SupaBase:BucketName.</param>
    Task<byte[]> DownloadRawAsync(string objectPath, string? bucket = null);
}
