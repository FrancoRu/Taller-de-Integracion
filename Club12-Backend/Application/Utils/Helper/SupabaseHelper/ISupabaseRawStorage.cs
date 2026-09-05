using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Application.Utils.Helper.SupabaseHelper;

/// <summary>
/// Narrow raw-storage boundary over the Supabase Storage client, implemented by SupabaseHelper.
/// </summary>
public interface ISupabaseRawStorage
{
    /// <summary>
    /// Uploads raw content to objectPath in bucket.
    /// </summary>
    /// <param name="objectPath">The exact destination path within the bucket.</param>
    /// <param name="content">The content stream to upload.</param>
    /// <param name="bucket">Target bucket; null uses the configured SupaBase:BucketName.</param>
    Task UploadRawAsync(string objectPath, Stream content, string? bucket = null);

    /// <summary>
    /// Lists raw objects under prefix in bucket.
    /// </summary>
    /// <param name="prefix">The bucket-relative path prefix to list.</param>
    /// <param name="bucket">Target bucket; null uses the configured SupaBase:BucketName.</param>
    Task<IReadOnlyList<SupabaseStorageEntry>> ListRawAsync(string prefix, string? bucket = null);

    /// <summary>
    /// Removes the raw object at objectPath in bucket.
    /// </summary>
    /// <param name="objectPath">The exact object path within the bucket.</param>
    /// <param name="bucket">Target bucket; null uses the configured SupaBase:BucketName.</param>
    Task RemoveRawAsync(string objectPath, string? bucket = null);

    /// <summary>
    /// Downloads the raw content of the object at objectPath in bucket.
    /// </summary>
    /// <param name="objectPath">The exact object path within the bucket.</param>
    /// <param name="bucket">Target bucket; null uses the configured SupaBase:BucketName.</param>
    Task<byte[]> DownloadRawAsync(string objectPath, string? bucket = null);
}
