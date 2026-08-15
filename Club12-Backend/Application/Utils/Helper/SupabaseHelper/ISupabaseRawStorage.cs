using System;
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
    /// Uploads raw content to <paramref name="objectPath"/> in the configured bucket.
    /// </summary>
    Task UploadRawAsync(string objectPath, Stream content);

    /// <summary>
    /// Lists raw objects under <paramref name="prefix"/> in the configured bucket.
    /// </summary>
    Task<IReadOnlyList<SupabaseStorageEntry>> ListRawAsync(string prefix);

    /// <summary>
    /// Removes the raw object at <paramref name="objectPath"/> in the configured bucket.
    /// </summary>
    Task RemoveRawAsync(string objectPath);
}

/// <summary>
/// Minimal listing metadata for a raw Supabase Storage object, decoupled from
/// the 3rd-party Supabase.Storage.FileObject type so fakes don't need
/// to construct it. Name is relative to the queried prefix —
/// the same shape the Supabase Storage List API returns.
/// </summary>
public sealed record SupabaseStorageEntry(string Name, DateTimeOffset? UpdatedAt);
