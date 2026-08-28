using Application.Interfaces.Storage;
using Application.Utils.Helper.SupabaseHelper;

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Infrastructure.Storage;

/// <summary>
/// <see cref="IMedicalRecordStorage"/> implementation that reuses the existing
/// Supabase client/credentials (via <see cref="ISupabaseRawStorage"/>,
/// implemented by <see cref="SupabaseHelper"/>) instead of provisioning a
/// second Supabase client — mirroring how
/// <c>Infrastructure.Backup.SupabaseBackupStorage</c> reuses the same boundary
/// for backups. Confines every medical-record object to the dedicated
/// <c>medical-records/</c> prefix in the configured bucket, kept separate from
/// the <c>backups/</c> prefix (HU-56). File names are server-generated (a fresh
/// GUID preserving the original extension), so an upload can never overwrite an
/// unrelated object or traverse outside the prefix.
/// </summary>
public sealed class SupabaseMedicalRecordStorage(ISupabaseRawStorage rawStorage) : IMedicalRecordStorage
{
    private const string Prefix = "medical-records";

    public async Task<string> StoreAsync(
        Guid tournamentId, Guid playerId, string fileName, Stream content, CancellationToken ct = default)
    {
        string extension = Path.GetExtension(fileName);
        string objectPath = $"{Prefix}/{tournamentId}/{playerId}/{Guid.NewGuid()}{extension}";

        await rawStorage.UploadRawAsync(objectPath, content);

        return objectPath;
    }
}
