using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace Application.Utils.Constants.Validation;

/// <summary>
/// Validates that a string is a plausible Argentine phone number, mirroring
/// <c>isValidPhone</c> in <c>src/modules/core/utils/validators.ts</c>: only
/// digits, spaces, <c>+</c>, <c>-</c> and parentheses are allowed, and the
/// digits must total exactly 10 — the national format (area code + local
/// number) used for calls placed from inside the country, with no leading
/// <c>0</c> trunk prefix, no <c>15</c>, no <c>+54</c> country code and no
/// <c>9</c> mobile marker (those only apply to international dialing, which
/// this app — a local league — never needs).
/// A null or empty value passes (pair with <c>[Required]</c> when the field
/// itself is mandatory).
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
