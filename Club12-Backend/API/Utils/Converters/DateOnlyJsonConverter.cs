using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Club12.API.Utils.Converters;

/// <summary>
/// A custom JSON converter for <see cref="DateTime"/> that serializes and deserializes dates
/// in the format "dd/MM/yyyy".
/// </summary>
public class DateOnlyJsonConverter : JsonConverter<DateTime>
{
    /// <summary>
    /// The date format used for serialization and deserialization ("dd/MM/yyyy").
    /// </summary>
    private const string Format = "dd/MM/yyyy";

    /// <summary>
    /// Reads and converts the JSON string to a <see cref="DateTime"/> using the "dd/MM/yyyy" format.
    /// </summary>
    /// <param name="reader">The <see cref="Utf8JsonReader"/> to read from.</param>
    /// <param name="typeToConvert">The type to convert (expected to be <see cref="DateTime"/>).</param>
    /// <param name="options">The <see cref="JsonSerializerOptions"/> used during deserialization.</param>
    /// <returns>A <see cref="DateTime"/> parsed from the JSON string.</returns>
    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.GetDateTime();
    }

    /// <summary>
    /// Writes a <see cref="DateTime"/> value as a JSON string using the "dd/MM/yyyy" format.
    /// </summary>
    /// <param name="writer">The <see cref="Utf8JsonWriter"/> to write to.</param>
    /// <param name="value">The <see cref="DateTime"/> value to serialize.</param>
    /// <param name="options">The <see cref="JsonSerializerOptions"/> used during serialization.</param>
    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString(Format));
    }
}
