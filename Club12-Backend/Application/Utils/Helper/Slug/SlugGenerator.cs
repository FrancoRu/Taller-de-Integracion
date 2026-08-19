using System;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Application.Utils.Helper.Slug;

/// <summary>
/// Generates lowercase, kebab-case, URL-friendly slugs from display names.
/// </summary>
public static partial class SlugGenerator
{
    /// <summary>
    /// Converts <paramref name="text"/> into a lowercase, kebab-case slug: Spanish
    /// accents are transliterated to their base letter, every run of characters
    /// that is not a lowercase letter or digit becomes a single hyphen, and
    /// leading/trailing hyphens are trimmed.
    /// </summary>
    /// <param name="text">The display name to convert.</param>
    /// <returns>The generated slug. Never null, may be empty for input with no alphanumeric characters.</returns>
    public static string GenerateSlug(string text)
    {
        string normalized = text.Normalize(NormalizationForm.FormD);
        StringBuilder withoutAccents = new(normalized.Length);

        foreach (char character in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
            {
                withoutAccents.Append(character);
            }
        }

        string lowercase = withoutAccents.ToString().Normalize(NormalizationForm.FormC).ToLowerInvariant();
        string hyphenated = NonAlphanumericRunRegex().Replace(lowercase, "-");

        return hyphenated.Trim('-');
    }

    /// <summary>
    /// Generates a slug from <paramref name="text"/> and, when it collides with an
    /// existing slug, appends a numeric suffix (-2, -3, ...) until
    /// <paramref name="slugExists"/> reports no collision.
    /// </summary>
    /// <param name="text">The display name to convert.</param>
    /// <param name="slugExists">Predicate that checks whether a candidate slug is already taken.</param>
    /// <returns>A unique slug derived from <paramref name="text"/>.</returns>
    public static async Task<string> GenerateUniqueSlugAsync(string text, Func<string, Task<bool>> slugExists)
    {
        string baseSlug = GenerateSlug(text);
        string candidate = baseSlug;
        int suffix = 1;

        while (await slugExists(candidate))
        {
            suffix++;
            candidate = $"{baseSlug}-{suffix}";
        }

        return candidate;
    }

    [GeneratedRegex("[^a-z0-9]+")]
    private static partial Regex NonAlphanumericRunRegex();
}
