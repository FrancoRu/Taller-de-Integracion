namespace API.Utils;

/// <summary>
/// Maps file-extension enums to the actual dotted extension string
/// (e.g. <see cref="ImageFileExtension.Jpg"/> to ".jpg").
/// </summary>
public static class FileExtensionMappings
{
    public static string ToExtensionString(this ImageFileExtension extension)
    {
        return $".{extension.ToString().ToLowerInvariant()}";
    }

    public static string ToExtensionString(this SpreadsheetFileExtension extension)
    {
        return $".{extension.ToString().ToLowerInvariant()}";
    }
}
