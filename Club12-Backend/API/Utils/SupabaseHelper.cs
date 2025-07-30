namespace Club12.API.Utils;

/// <summary>
/// Provides helper methods to interact with Supabase storage,
/// including file upload and public URL generation.
/// </summary>
public class SupabaseHelper
{
    private readonly Supabase.Client _client;
    private readonly string _bucketName;
    private readonly string _baseUrl;

    /// <summary>
    /// Initializes a new instance of the <see cref="SupabaseHelper"/> class using configuration values.
    /// </summary>
    /// <param name="configuration">The application configuration containing Supabase settings.</param>
    public SupabaseHelper(IConfiguration configuration)
    {
        var section = configuration.GetSection("SupaBase");
        _baseUrl = section["ProjectUrl"]!;
        var serviceRole = section["ServiceRole"]!;
        _bucketName = section["BucketName"]!;

        var options = new Supabase.SupabaseOptions
        {
            AutoRefreshToken = true,
            AutoConnectRealtime = true
        };

        _client = new Supabase.Client(_baseUrl, serviceRole, options);
        _client.InitializeAsync().Wait();
    }

    /// <summary>
    /// Uploads an image stream to Supabase Storage.
    /// </summary>
    /// <param name="fileStream">The image stream to upload.</param>
    /// <param name="fileName">The destination filename in the bucket.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the upload fails.</exception>
    public async Task UploadImageAsync(Stream fileStream, string fileName)
    {
        try
        {
            await _client.Storage
                .From(_bucketName)
                .Upload(UseStreamDotReadMethod(fileStream), fileName,
                    new()
                    {
                        Upsert = true
                    }
                );
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error uploading file: {ex.Message}", ex);
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
    public async Task DeleteImageAsync(string fileName)
    {
        try
        {
            await _client.Storage.From(_bucketName).Remove(fileName);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error uploading file: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Generates a public URL for an uploaded file in Supabase Storage.
    /// </summary>
    /// <param name="fileName">The name of the file stored in the bucket.</param>
    /// <returns>A publicly accessible URL for the file.</returns>
    public string GetPublicUrl(string fileName)
    {
        return $"{_baseUrl}/storage/v1/object/public/{_bucketName}/{fileName}";
    }

    /// <summary>
    /// Reads all bytes from a given <see cref="Stream"/> using the <c>Stream.Read</c> method in chunks,
    /// and returns the entire content as a byte array.
    /// </summary>
    /// <param name="stream">The input stream to read from.</param>
    /// <returns>A byte array containing all data read from the stream.</returns>
    private static byte[] UseStreamDotReadMethod(Stream stream)
    {
        List<byte> totalStream = new();
        byte[] buffer = new byte[32];
        int read;
        while ((read = stream.Read(buffer, 0, buffer.Length)) > 0)
        {
            totalStream.AddRange(buffer[..read]);
        }
        return totalStream.ToArray();
    }

}
