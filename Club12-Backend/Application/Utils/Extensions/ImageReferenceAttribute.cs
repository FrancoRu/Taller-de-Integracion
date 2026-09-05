using System;
using System.ComponentModel.DataAnnotations;

namespace Application.Utils.Extensions;

/// <summary>
/// Validates a property that carries an image reference: either an absolute http or https URL or a self-contained data:image URI.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
public sealed class ImageReferenceAttribute : ValidationAttribute
{
    private const string DataImagePrefix = "data:image/";

    /// <inheritdoc/>
    public override bool IsValid(object? value)
    {
        // Null or empty means no image, which is the property's own business, decided by a Required attribute alongside this one.
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
