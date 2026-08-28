using Application.Utils.Constants;
using Application.Utils.Constants.Configuration;
using Application.Utils.Helper.SupabaseHelper;

using Microsoft.Extensions.Configuration;

using Supabase;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Infrastructure.Storage;

/// <summary>
/// Provides helper methods to interact with Supabase storage,
/// including file upload and public URL generation.
/// </summary>
public class SupabaseHelper : ISupabaseRawStorage
{
    /// <summary>
    /// Cache-Control max-age, in seconds, applied to uploaded images (1 year —
    /// uploaded files are content-addressed by a fresh GUID name, so they
    /// never need to be invalidated).
    /// </summary>
    private const string ImageCacheControlMaxAgeSeconds = "31536000";

    private readonly Client _client;
    private readonly string _bucketName;

    /// <summary>
    /// Initializes a new instance of the SupabaseHelper class using configuration values.
    /// </summary>
    /// <param name="configuration">The application configuration containing Supabase settings.</param>
    public SupabaseHelper(IConfiguration configuration)
    {
        IConfigurationSection section = configuration.GetSection(ConfigurationKeys.Supabase.Section);
        string baseUrl = section[ConfigurationKeys.Supabase.ProjectUrl]!;
        string serviceRole = section[ConfigurationKeys.Supabase.ServiceRole]!;
        _bucketName = section[ConfigurationKeys.Supabase.BucketName]!;

        SupabaseOptions options = new()
        {
            AutoRefreshToken = true,
            AutoConnectRealtime = true
        };

        _client = new Client(baseUrl, serviceRole, options);
        _client.InitializeAsync().Wait();
    }

    /// <summary>
    /// Uploads an image stream to Supabase Storage.
    /// </summary>
    /// <param name="fileStream">The image stream to upload.</param>
    /// <param name="fileName">The destination filename in the bucket.</param>
    /// <returns>A string representing the new file name.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the upload fails.</exception>
    public async Task<string> UploadImageAsync<T>(Stream fileStream, string fileName)
    {
        try
        {
            string newFileName = GenerateNameFile(fileName);
            string fullPathInBucket = $"{typeof(T).Name}/{newFileName}";
            await _client.Storage
                .From(_bucketName)
                .Upload(UseStreamDotReadMethod(fileStream), fullPathInBucket,
                    new()
                    {
                        CacheControl = ImageCacheControlMaxAgeSeconds,
                        Upsert = true
                    }
                );
            return _client.Storage.From(_bucketName).GetPublicUrl(fullPathInBucket);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(ErrorMessages.Storage.UploadFailed(ex.Message), ex);
        }
    }

    /// <summary>
    /// Deletes an image from the specified storage bucket.
    /// </summary>
    /// <param name="fileName">The name of the file to delete within the bucket.</param>
    /// <returns>A task that represents the asynchronous delete operation.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when an error occurs while deleting the file.
    /// </exception>
    public async Task DeleteImageAsync<T>(string fileName)
    {
        try
        {
            await _client.Storage.From(_bucketName).Remove($"{typeof(T).Name}/{fileName}");
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(ErrorMessages.Storage.DeleteFailed(ex.Message), ex);
        }
    }

    /// <summary>
    /// Uploads raw content to an arbitrary object path in the bucket. Unlike
    /// UploadImageAsync{T}, this is not image-shaped: no
    /// per-type folder convention, no cache-control header, no public-URL
    /// return — the caller supplies the exact destination path. Additive:
    /// UploadImageAsync{T}'s behavior and call sites are
    /// untouched.
    /// </summary>
    /// <param name="objectPath">The exact destination path within the bucket.</param>
    /// <param name="content">The content stream to upload.</param>
    /// <exception cref="InvalidOperationException">Thrown when the upload fails.</exception>
    public async Task UploadRawAsync(string objectPath, Stream content)
    {
        try
        {
            await _client.Storage
                .From(_bucketName)
                .Upload(UseStreamDotReadMethod(content), objectPath, new() { Upsert = true });
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(ErrorMessages.Storage.UploadFailed(ex.Message), ex);
        }
    }

    /// <summary>
    /// Lists raw objects under <paramref name="prefix"/> in the bucket.
    /// Additive: does not affect UploadImageAsync{T} or
    /// DeleteImageAsync{T}.
    /// </summary>
    /// <param name="prefix">The bucket-relative path prefix to list.</param>
    /// <exception cref="InvalidOperationException">Thrown when listing fails.</exception>
    public async Task<IReadOnlyList<SupabaseStorageEntry>> ListRawAsync(string prefix)
    {
        try
        {
            List<Supabase.Storage.FileObject> files = await _client.Storage.From(_bucketName).List(prefix)
                ?? [];
            return files
                .Select(file => new SupabaseStorageEntry(
                    file.Name ?? string.Empty,
                    file.UpdatedAt.HasValue
                        ? new DateTimeOffset(DateTime.SpecifyKind(file.UpdatedAt.Value, DateTimeKind.Utc))
                        : null))
                .ToList();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(ErrorMessages.Storage.ListFailed(ex.Message), ex);
        }
    }

    /// <summary>
    /// Removes the raw object at <paramref name="objectPath"/> in the bucket.
    /// Additive: existing DeleteImageAsync{T} behavior and call
    /// sites are untouched.
    /// </summary>
    /// <param name="objectPath">The exact object path within the bucket.</param>
    /// <exception cref="InvalidOperationException">Thrown when the removal fails.</exception>
    public async Task RemoveRawAsync(string objectPath)
    {
        try
        {
            await _client.Storage.From(_bucketName).Remove(objectPath);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(ErrorMessages.Storage.RemoveFailed(ex.Message), ex);
        }
    }

    /// <summary>
    /// Downloads the raw object at <paramref name="objectPath"/> from the
    /// bucket and returns its bytes as a readable stream. Additive: does not
    /// affect the image-shaped helpers or the other raw operations.
    /// </summary>
    /// <param name="objectPath">The exact object path within the bucket.</param>
    /// <exception cref="InvalidOperationException">Thrown when the download fails.</exception>
    public async Task<Stream> DownloadRawAsync(string objectPath)
    {
        try
        {
            byte[] bytes = await _client.Storage.From(_bucketName).Download(objectPath, (EventHandler<float>?)null);
            return new MemoryStream(bytes);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(ErrorMessages.Storage.ListFailed(ex.Message), ex);
        }
    }

    /// <summary>
    /// Reads all bytes from a given Stream using the Stream.Read method in chunks,
    /// and returns the entire content as a byte array.
    /// </summary>
    /// <param name="stream">The input stream to read from.</param>
    /// <returns>A byte array containing all data read from the stream.</returns>
    private static byte[] UseStreamDotReadMethod(Stream stream)
    {
        List<byte> totalStream = [];
        byte[] buffer = new byte[32];
        int read;
        while ((read = stream.Read(buffer, 0, buffer.Length)) > 0)
        {
            totalStream.AddRange(buffer[..read]);
        }
        return [.. totalStream];
    }

    /// <summary>
    /// Generates a new unique file name by replacing the original name with a GUID,
    /// preserving the original file extension.
    /// </summary>
    /// <param name="fileName">The original file name including extension.</param>
    /// <returns>A new file name string consisting of a GUID and the original extension.</returns>
    private static string GenerateNameFile(string fileName)
    {
        string extension = Path.GetExtension(fileName);
        string newName = Guid.NewGuid().ToString();

        return $"{newName}{extension}";
    }

}
