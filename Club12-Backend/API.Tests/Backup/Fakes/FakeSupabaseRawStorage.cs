using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Application.Utils.Helper.SupabaseHelper;

namespace API.Tests.Backup.Fakes;

/// <summary>
/// Test double for ISupabaseRawStorage. Records the exact
/// object paths passed to each raw call and can be configured to throw
/// (simulating a network/auth error from the real Supabase client), so
/// SupabaseBackupStorage can be unit tested without a real SupabaseHelper (whose constructor performs
/// real network initialization) or any network call — per this change's spec
/// Non-Goal on actual Supabase upload verification.
/// </summary>
public sealed class FakeSupabaseRawStorage : ISupabaseRawStorage
{
    public List<string> UploadedPaths { get; } = [];
    public List<string> RemovedPaths { get; } = [];
    public string? LastListedPrefix { get; private set; }

    public IReadOnlyList<SupabaseStorageEntry> EntriesToList { get; set; } = Array.Empty<SupabaseStorageEntry>();

    /// <summary>
    /// When set, every raw call throws this exception instead of succeeding —
    /// simulates a network error, auth error, or any other Supabase client
    /// failure.
    /// </summary>
    public Exception? ExceptionToThrow { get; set; }

    public Task UploadRawAsync(string objectPath, Stream content)
    {
        if (ExceptionToThrow is not null)
            throw ExceptionToThrow;

        UploadedPaths.Add(objectPath);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<SupabaseStorageEntry>> ListRawAsync(string prefix)
    {
        if (ExceptionToThrow is not null)
            throw ExceptionToThrow;

        LastListedPrefix = prefix;
        return Task.FromResult(EntriesToList);
    }

    public Task RemoveRawAsync(string objectPath)
    {
        if (ExceptionToThrow is not null)
            throw ExceptionToThrow;

        RemovedPaths.Add(objectPath);
        return Task.CompletedTask;
    }
}
