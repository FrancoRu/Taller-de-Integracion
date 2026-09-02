using System;
using System.ComponentModel.DataAnnotations;

namespace Application.Utils.Extensions;

/// <summary>
/// Validates a property that carries an image reference: either an absolute
/// http(s) URL (an uploaded file in the storage bucket) or a self-contained
/// <c>data:image/...</c> URI.
///
/// Exists because <see cref="UrlAttribute"/> accepts only http, https and ftp,
/// so a row whose picture is a generated inline SVG — every seeded venue —
/// could be read back and displayed but never saved again: re-submitting the
/// edit form unchanged failed validation with 400.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
public sealed class ImageReferenceAttribute : ValidationAttribute
{
    private const string DataImagePrefix = "data:image/";

    /// <inheritdoc/>
    public override bool IsValid(object? value)
    {
        // Null/empty means "no image", which is the property's own business
        // (a [Required] alongside this one decides that).
        if (value is null)
        {
            return true;
        }

        if (value is not string reference || string.IsNullOrWhiteSpace(reference))
        {
            return value is not string;
        }

        if (reference.StartsWith(DataImagePrefix, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return Uri.TryCreate(reference, UriKind.Absolute, out Uri? uri)
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }

    /// <inheritdoc/>
    public override string FormatErrorMessage(string name) =>
        $"The {name} field must be an http(s) URL or a data:image URI.";
}
