using Application.Utils.Constants;

using System;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace API.Utils.Converters;

/// <summary>
/// A custom JSON converter for DateTime. Accepts a wide range of common input
/// formats when reading (see <see cref="_formats"/>), but always writes using a
/// single canonical format ("yyyy-MM-ddTHH:mm:ss.fffZ") so output is consistent
/// regardless of which format the value was originally read from.
/// </summary>
public class DateOnlyJsonConverter : JsonConverter<DateTime>
{

    /// <summary>
    /// The formats accepted when parsing a date from JSON, tried in order. Only
    /// used for reading — <see cref="Write"/> always emits a single canonical format.
    /// </summary>
    private static readonly string[] _formats =
    [
        "dd/MM/yyyy",
        "MM/dd/yyyy",
        "yyyy-MM-dd",
        "yyyy/MM/dd",
        "yyyyMMdd",
        "dd-MM-yyyy",
        "MM-dd-yyyy",
        "dd.MM.yyyy",
        "yyyy-MM-ddTHH:mm:ss",
        "yyyy-MM-ddTHH:mm:ss.fff",
        "yyyy-MM-ddTHH:mm:ssZ",
        "yyyy-MM-ddTHH:mm:ss.fffZ",
        "o",
        "s",
        "yyyy-MM-ddTHH:mm"
    ];

    /// <summary>
    /// Reads and converts the JSON string to a DateTime, trying each of the
    /// accepted <see cref="_formats"/> in order until one matches.
    /// </summary>
    /// <param name="reader">The Utf8JsonReader to read from.</param>
    /// <param name="typeToConvert">The type to convert (expected to be DateTime).</param>
    /// <param name="options">The JsonSerializerOptions used during deserialization.</param>
    /// <returns>A DateTime parsed from the JSON string.</returns>
    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        string? dateString = reader.GetString();

        return DateTime.TryParseExact(
                dateString,
                _formats,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out DateTime parsedDate)
            ? DateTime.SpecifyKind(parsedDate, DateTimeKind.Unspecified)
            : throw new JsonException(ErrorMessages.Serialization.InvalidDate(dateString));
    }

    /// <summary>
    /// Writes a DateTime value as a JSON string using the canonical
    /// "yyyy-MM-ddTHH:mm:ss.fffZ" format, regardless of which accepted format
    /// it was originally read from.
    /// </summary>
    /// <param name="writer">The Utf8JsonWriter to write to.</param>
    /// <param name="value">The DateTime value to serialize.</param>
    /// <param name="options">The JsonSerializerOptions used during serialization.</param>
    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString("yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture));
    }
}
