namespace Services.Utils.Cloudfare;

public interface ICloudflareService
{
    /// <summary>
    /// Uploads a logo image to Cloudflare and returns the URL.
    /// </summary>
    /// <param name="file">The file stream containing the image data.</param>
    /// <param name="fileName">The name of the image file (should end with .jpeg or .png).</param>
    /// <returns>The URL of the uploaded image.</returns>
    Task<string> UploadLogoAsync(Stream file, string fileName);
}
