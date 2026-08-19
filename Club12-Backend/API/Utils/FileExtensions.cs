using Microsoft.AspNetCore.Http;

using System;
using System.IO;

namespace API.Utils;

/// <summary>
/// Provides extension methods for file validation.
/// </summary>
public static class FileExtensions
{
    private static readonly ImageFileExtension[] _validImageExtensions =
    [
        ImageFileExtension.Jpg,
        ImageFileExtension.Jpeg,
        ImageFileExtension.Png,
        ImageFileExtension.Webp,
    ];

    private static readonly SpreadsheetFileExtension[] _validSpreadsheetExtensions =
    [
        SpreadsheetFileExtension.Xls,
        SpreadsheetFileExtension.Xlsx,
    ];

    /// <summary>
    /// Checks if the provided file is a valid image (JPEG, PNG, or WEBP).
    /// </summary>
    /// <param name="file">The uploaded file to check.</param>
    /// <returns>True if the file is a valid image, otherwise false.</returns>
    public static bool IsValidImageFile(this IFormFile file)
    {
        if (file is null || file.Length is 0)
        {
            return false;
        }

        string fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();

        return Array.Exists(_validImageExtensions, extension => extension.ToExtensionString() == fileExtension);
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

        string fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();

        return Array.Exists(_validSpreadsheetExtensions, extension => extension.ToExtensionString() == fileExtension);
    }
}
