using Microsoft.Extensions.Configuration;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;

namespace Club12.Services.Utils.Cloudfare.Implementation;

public class CloudflareService : ICloudflareService
{
    private readonly HttpClient _httpClient;
    private readonly string _cloudflareApiUrl;
    private readonly string _cloudflareAccountId;
    private readonly string _cloudflareApiToken;

    /// <summary>
    /// Initializes a new instance of the <see cref="CloudflareService"/> class.
    /// </summary>
    /// <param name="httpClient">The HTTP client instance.</param>
    /// <param name="configuration">Configuration object to access Cloudflare API settings.</param>
    public CloudflareService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _cloudflareApiUrl = configuration["Cloudflare:ApiUrl"] ?? throw new ArgumentNullException("Cloudflare:ApiUrl", "API URL cannot be null.");
        _cloudflareAccountId = configuration["Cloudflare:AccountId"] ?? throw new ArgumentNullException("Cloudflare:AccountId", "Account ID cannot be null.");
        _cloudflareApiToken = configuration["Cloudflare:ApiToken"] ?? throw new ArgumentNullException("Cloudflare:ApiToken", "API Token cannot be null.");
    }

    /// <summary>
    /// Uploads a logo image to Cloudflare and returns the URL.
    /// </summary>
    /// <param name="file">The file stream containing the image data.</param>
    /// <param name="fileName">The name of the image file (should end with .jpeg or .png).</param>
    /// <returns>The URL of the uploaded image.</returns>
    public async Task<string> UploadLogoAsync(Stream file, string fileName)
    {
        ValidateFile(file, fileName);

        using var content = new MultipartFormDataContent
        {
            { new StreamContent(file), "file", fileName }
        };

        var request = CreateRequest(content);
        var response = await SendRequestAsync(request);

        return ExtractUrlFromResponse(response);
    }

    /// <summary>
    /// Uploads an image from a URL to Cloudflare and returns the image URL.
    /// </summary>
    /// <param name="imageUrl">The URL of the image to upload.</param>
    /// <param name="metadata">Optional metadata for the image.</param>
    /// <returns>The URL of the uploaded image.</returns>
    public async Task<string> UploadImageFromUrlAsync(string imageUrl, string metadata = null)
    {
        if (string.IsNullOrWhiteSpace(imageUrl))
        {
            throw new ArgumentException("Image URL cannot be null or empty.", nameof(imageUrl));
        }

        using var content = new MultipartFormDataContent
        {
            { new StringContent(imageUrl), "url" },
            { new StringContent(metadata ?? "{}"), "metadata" }
        };

        var request = CreateRequest(content);
        var response = await SendRequestAsync(request);

        return ExtractUrlFromResponse(response);
    }

    private void ValidateFile(Stream file, string fileName)
    {
        if (file is null)
        {
            throw new ArgumentNullException(nameof(file), "File stream cannot be null.");
        }

        if (string.IsNullOrWhiteSpace(fileName) ||
            !(fileName.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase) || fileName.EndsWith(".png", StringComparison.OrdinalIgnoreCase)))
        {
            throw new ArgumentException("File name must be a non-empty string ending with .jpeg or .png.", nameof(fileName));
        }
    }

    private HttpRequestMessage CreateRequest(MultipartFormDataContent content)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, $"{_cloudflareApiUrl}/accounts/{_cloudflareAccountId}/images/v1")
        {
            Content = content
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _cloudflareApiToken);
        return request;
    }

    private async Task<string> SendRequestAsync(HttpRequestMessage request)
    {
        HttpResponseMessage response = await _httpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            string responseBody = await response.Content.ReadAsStringAsync();
            throw new InvalidOperationException($"Failed to upload the logo to Cloudflare. Status Code: {response.StatusCode}. Response: {responseBody}");
        }

        return await response.Content.ReadAsStringAsync();
    }

    private static string ExtractUrlFromResponse(string jsonResponse)
    {
        var jsonObject = JsonSerializer.Deserialize<JsonElement>(jsonResponse);
        if (jsonObject.TryGetProperty("result", out var result) && result.TryGetProperty("variants", out var variants) && variants[0].TryGetString(out var url))
        {
            return url;
        }

        throw new InvalidOperationException("URL was not found in the response.");
    }
}
