using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace Application.Utils.Constants.Validation;

/// <summary>
/// Validates that a string is a plausible Argentine phone number, mirroring
/// <c>isValidPhone</c> in <c>src/modules/core/utils/validators.ts</c>: only
/// digits, spaces, <c>+</c>, <c>-</c> and parentheses are allowed, and the
/// digit count/prefix combination must match how Argentine numbers are
/// actually written:
/// - 10 digits: a bare local number (area code + line, no prefix).
/// - 11 digits: a mobile marked with a leading <c>9</c>, or a local number
///   with the domestic long-distance <c>0</c> trunk prefix.
/// - 12 digits: the <c>54</c> country code plus a landline (no mobile marker).
/// - 13 digits: the <c>549</c> country code plus the mobile marker.
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
        string digits = new(trimmed.Where(char.IsDigit).ToArray());

        bool isValid = hasOnlyAllowedChars && digits.Length switch
        {
            10 => true,
            11 => digits.StartsWith('9') || digits.StartsWith('0'),
            12 => digits.StartsWith("54") && !digits.StartsWith("549"),
            13 => digits.StartsWith("549"),
            _ => false,
        };

        return isValid
            ? ValidationResult.Success
            : new ValidationResult(
                ErrorMessage ?? ValidationPatterns.PhoneNumberError,
                [validationContext.MemberName ?? string.Empty]);
    }
}
