namespace Club12.API.Utils;

/// <summary>
/// Provides extension methods for file validation.
/// </summary>
public static class FileExtensions
{
    /// <summary>
    /// Checks if the provided file is a valid image (JPEG or PNG).
    /// </summary>
    /// <param name="file">The uploaded file to check.</param>
    /// <returns>True if the file is a valid image, otherwise false.</returns>
    public static bool IsValidImageFile(this IFormFile file)
    {
        if (file is null || file.Length is 0)
        {
            return false;
        }

        string[] validExtensions = [".jpg", ".jpeg", ".png"];
        string fileExtension = Path.GetExtension(file.FileName).ToLower();

        return validExtensions.Contains(fileExtension);
    }

    /// <summary>
    /// Checks if the provided file is a valid Excel file (XLSX or XLS).
    /// </summary>
    /// <param name="file">The uploaded file to check.</param>
    /// <returns>True if the file is a valid Excel file, otherwise false.</returns>
    public static bool IsValidExcelFile(this IFormFile file)
    {
        if (file is null || file.Length is 0)
        {
            return false;
        }

        string[] validExtensions = [".xls", ".xlsx"];
        string fileExtension = Path.GetExtension(file.FileName).ToLower();

        return validExtensions.Contains(fileExtension);
    }
}
