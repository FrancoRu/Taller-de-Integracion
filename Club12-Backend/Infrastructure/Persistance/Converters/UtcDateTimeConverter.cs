using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

using System;

namespace Infrastructure.Persistance.Converters;

/// <summary>
/// Ensures DateTime values are always read from the database as UTC.
/// Writes the value as Unspecified to satisfy 'timestamp without time zone'.
/// </summary>
public class UtcDateTimeConverter()
    : ValueConverter<DateTime, DateTime>(
        v => DateTime.SpecifyKind(v, DateTimeKind.Unspecified),
        v => DateTime.SpecifyKind(v, DateTimeKind.Utc));

/// <summary>
/// Nullable variant of UtcDateTimeConverter.
/// </summary>
public class NullableUtcDateTimeConverter()
    : ValueConverter<DateTime?, DateTime?>(
        v => v.HasValue ? DateTime.SpecifyKind(v.Value, DateTimeKind.Unspecified) : null,
        v => v.HasValue ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : null);