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
