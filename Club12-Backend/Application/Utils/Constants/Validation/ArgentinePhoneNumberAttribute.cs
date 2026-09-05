using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace Application.Utils.Constants.Validation;

/// <summary>
/// Validates that a string is a plausible Argentine phone number.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public sealed class ArgentinePhoneNumberAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is not string phone || string.IsNullOrWhiteSpace(phone))
        {
            return ValidationResult.Success;
        }

        string trimmed = phone.Trim();
        bool hasOnlyAllowedChars = trimmed.All(c => char.IsDigit(c) || c is '+' or ' ' or '(' or ')' or '-');
        int digitCount = trimmed.Count(char.IsDigit);

        return hasOnlyAllowedChars && digitCount == 10
            ? ValidationResult.Success
            : new ValidationResult(
                ErrorMessage ?? ValidationPatterns.PhoneNumberError,
                [validationContext.MemberName ?? string.Empty]);
    }
}
