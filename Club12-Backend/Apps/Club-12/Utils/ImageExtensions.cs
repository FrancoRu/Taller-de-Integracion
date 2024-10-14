namespace Club12.Extensions;

/// <summary>
/// Provides extension methods for file validation.
/// </summary>
public static class ImageExtensions
{
    /// <summary>
    /// Checks if the provided file is a valid image (JPEG or PNG).
    /// </summary>
    /// <param name="file">The uploaded file to check.</param>
    /// <returns>True if the file is a valid image, otherwise false.</returns>
    public static bool IsValidImageFile(this IFormFile file)
    {
        if (file is null || file.Length == 0)
            return false;

        string[] validExtensions = [".jpg", ".jpeg", ".png"];
        string fileExtension = Path.GetExtension(file.FileName).ToLower();

        return validExtensions.Contains(fileExtension);
    }
}
