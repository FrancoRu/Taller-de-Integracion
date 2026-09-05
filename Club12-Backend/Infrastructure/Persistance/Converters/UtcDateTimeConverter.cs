using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

using System;

namespace Infrastructure.Persistance.Converters;

/// <summary>
/// Converts DateTime values to Unspecified for storage and back to UTC on read, to satisfy the timestamp without time zone column type.
/// </summary>
public class UtcDateTimeConverter()
    : ValueConverter<DateTime, DateTime>(
        v => DateTime.SpecifyKind(v, DateTimeKind.Unspecified),
        v => DateTime.SpecifyKind(v, DateTimeKind.Utc));
