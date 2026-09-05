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
/// IMedicalRecordStorage implementation that reuses the existing Supabase client through ISupabaseRawStorage, storing every medical record in a dedicated private bucket separate from the shared public-images bucket.
/// </summary>
public sealed class SupabaseMedicalRecordStorage : IMedicalRecordStorage
{
    /// <summary>
    /// Private bucket, provisioned manually per environment and never created by the client.
    /// </summary>
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
