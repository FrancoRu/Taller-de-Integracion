namespace Application.Utils.Constants.Validation;

/// <summary>
/// Shared validation regex patterns kept in one place so every request DTO —
/// and the frontend validators in <c>src/modules/core/utils/validators.ts</c> —
/// enforce the exact same "plausible" contact-field rules. Keeping them in sync
/// means the client never lets through a value the server will reject with 400.
/// </summary>
public static class ValidationPatterns
{
    /// <summary>
    /// Plausible phone number: only digits, spaces, <c>+</c>, <c>-</c> and
    /// parentheses, containing between 8 and 15 digits. The leading lookahead
    /// counts the digits regardless of how the separators are placed.
    /// </summary>
    public const string PhoneNumber = @"^(?=(?:\D*\d){8,15}\D*$)[+\d\s()-]+$";

    /// <summary>Human-facing message for a phone number that fails the pattern.</summary>
    public const string PhoneNumberError = "Invalid phone number format.";
}
