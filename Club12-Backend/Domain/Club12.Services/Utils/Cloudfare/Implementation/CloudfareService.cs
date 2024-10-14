namespace Club12.Services.Utils.Cloudfare;

/// <summary>
/// Mock implementation of ICloudflareService that generates a mock URL for testing purposes.
/// </summary>
public class CloudflareService : ICloudflareService
{
    /// <summary>
    /// Mock implementation that returns a generated mock URL for the uploaded logo image.
    /// </summary>
    /// <param name="file">The file stream (ignored in mock).</param>
    /// <param name="fileName">The name of the image file (ignored in mock).</param>
    /// <returns>A mock URL for the uploaded image.</returns>
    public Task<string> UploadLogoAsync(Stream file, string fileName)
    {
        return Task.FromResult($"https://mock.cloudflare.com/images/{Guid.NewGuid()}.jpeg");
    }
}
