using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

using System;

namespace Infrastructure.Persistance.Converters;

/// <summary>
/// Nullable variant of UtcDateTimeConverter.
/// </summary>
public class NullableUtcDateTimeConverter()
    : ValueConverter<DateTime?, DateTime?>(
        v => v.HasValue ? DateTime.SpecifyKind(v.Value, DateTimeKind.Unspecified) : null,
        v => v.HasValue ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : null);
