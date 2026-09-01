using System;
using System.ComponentModel.DataAnnotations;

namespace Application.Utils.Constants.Validation;

/// <summary>
/// Validates that a <see cref="DateTime"/> (or <see cref="Nullable{T}"/> of it)
/// birth date is at least <c>minimumYears</c> years in the past, i.e. the
/// person is at least that many years old today. A null value passes (pair
/// with <c>[Required]</c> when the field itself is mandatory) so this
/// attribute works unchanged on the optional <c>BirthDate</c> in an update
/// request.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public sealed class MinimumAgeAttribute(int minimumYears) : ValidationAttribute
{
    private readonly int _minimumYears = minimumYears;

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is not DateTime birthDate)
        {
            return ValidationResult.Success;
        }

        DateTime cutoff = DateTime.UtcNow.Date.AddYears(-_minimumYears);

        return birthDate.Date <= cutoff
            ? ValidationResult.Success
            : new ValidationResult(
                ErrorMessage ?? $"El jugador debe tener al menos {_minimumYears} años.",
                [validationContext.MemberName ?? string.Empty]);
    }
}
