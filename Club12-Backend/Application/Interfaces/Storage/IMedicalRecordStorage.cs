using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Interfaces.Storage;

/// <summary>
/// Storage boundary for player medical-record files (PDF, HU-55/HU-56).
/// Confines every file to a dedicated medical-records area, separate from
/// database backups (which use their own <c>backups/</c> area). Implemented in
/// Infrastructure over the shared Supabase client so the upload flow can be
/// wired independently of, and unit-tested without, a live Supabase bucket.
/// </summary>
public interface IMedicalRecordStorage
{
    /// <summary>
    /// Stores a medical-record file for a player's season registration and
    /// returns the storage reference (object path) to persist on the
    /// registration. The object is placed under a per-season, per-player path
    /// so the same player in another team/tournament keeps a separate file
    /// (HU-55/HU-59).
    /// </summary>
    /// <param name="tournamentId">The season the record belongs to.</param>
    /// <param name="playerId">The player the record belongs to.</param>
    /// <param name="fileName">The original file name (its extension is preserved).</param>
    /// <param name="content">The file content stream.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The stored object path used as the persisted file reference.</returns>
    Task<string> StoreAsync(Guid tournamentId, Guid playerId, string fileName, Stream content, CancellationToken ct = default);
}
