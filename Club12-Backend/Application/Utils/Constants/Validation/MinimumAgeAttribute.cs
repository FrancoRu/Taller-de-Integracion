using System;
using System.ComponentModel.DataAnnotations;

namespace Application.Utils.Constants.Validation;

/// <summary>
/// Validates that a DateTime birth date is at least minimumYears years in the past, meaning the person is at least that many years old today.
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
