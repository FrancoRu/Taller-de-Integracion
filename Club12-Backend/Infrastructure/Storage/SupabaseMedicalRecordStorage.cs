using Application.Interfaces.Storage;
using Application.Utils.Constants.Configuration;
using Application.Utils.Helper.SupabaseHelper;

using Microsoft.Extensions.Configuration;

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
/// for backups. Every medical-record object is stored in a dedicated
/// <b>private</b> bucket (<see cref="DefaultBucketName"/> by default,
/// overridable via <c>SupaBase:MedicalRecordsBucketName</c>), separate from
/// the shared <c>public-images</c> bucket used for team logos, blog images,
/// and (via <c>SupabaseBackupStorage</c>) database backups. The object key is
/// <c>{teamId}/{playerId}/{guid}{ext}</c> — server-generated, so an upload can
/// never overwrite an unrelated object or traverse outside the medical bucket.
/// </summary>
public sealed class SupabaseMedicalRecordStorage : IMedicalRecordStorage
{
    /// <summary>Private bucket, provisioned manually per environment. Never created by the client.</summary>
    public const string DefaultBucketName = "medical-records";

    private readonly ISupabaseRawStorage _rawStorage;
    private readonly string _bucketName;

    public SupabaseMedicalRecordStorage(ISupabaseRawStorage rawStorage, IConfiguration configuration)
    {
        _rawStorage = rawStorage;
        IConfigurationSection section = configuration.GetSection(ConfigurationKeys.Supabase.Section);
        string? configured = section[ConfigurationKeys.Supabase.MedicalRecordsBucketName];
        _bucketName = string.IsNullOrWhiteSpace(configured) ? DefaultBucketName : configured;
    }

    public async Task<string> StoreAsync(
        Guid teamId, Guid playerId, string fileName, Stream content, CancellationToken ct = default)
    {
        string extension = Path.GetExtension(fileName);
        string objectPath = $"{teamId}/{playerId}/{Guid.NewGuid()}{extension}";

        await _rawStorage.UploadRawAsync(objectPath, content, _bucketName);

        return objectPath;
    }

    public async Task<byte[]> DownloadAsync(string objectPath, CancellationToken ct = default)
    {
        return await _rawStorage.DownloadRawAsync(objectPath, _bucketName);
    }
}
