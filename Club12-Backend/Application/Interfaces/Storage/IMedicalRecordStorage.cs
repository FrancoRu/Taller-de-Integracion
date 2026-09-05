using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Interfaces.Storage;

/// <summary>
/// Storage boundary for player medical-record files, PDF.
/// </summary>
public interface IMedicalRecordStorage
{
    /// <summary>
    /// Stores a medical-record file for a player's team registration and returns the storage reference to persist on the registration.
    /// </summary>
    /// <param name="teamId">The team the registration belongs to.</param>
    /// <param name="playerId">The player the record belongs to.</param>
    /// <param name="fileName">The original file name, its extension is preserved.</param>
    /// <param name="content">The file content stream.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The stored object path used as the persisted file reference.</returns>
    Task<string> StoreAsync(Guid teamId, Guid playerId, string fileName, Stream content, CancellationToken ct = default);

    /// <summary>
    /// Downloads the raw bytes of a previously stored medical-record object.
    /// </summary>
    /// <param name="objectPath">
    /// The stored object path returned by StoreAsync and persisted
    /// on the season registration as its file reference.
    /// </param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The raw file content.</returns>
    Task<byte[]> DownloadAsync(string objectPath, CancellationToken ct = default);
}
