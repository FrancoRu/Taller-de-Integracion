namespace Application.Utils.Constants.Validation;

/// <summary>
/// Shared validation regex patterns kept in one place so every request DTO and the frontend validators enforce the exact same plausible contact-field rules.
/// </summary>
public static class ValidationPatterns
{
    /// <summary>
    /// Plausible phone number: only digits, spaces, plus, hyphen and parentheses, containing between 8 and 15 digits.
    /// </summary>
    public const string PhoneNumber = @"^(?=(?:\D*\d){8,15}\D*$)[+\d\s()-]+$";

    /// <summary>
    /// Human-facing message for a phone number that fails the pattern.
    /// </summary>
    public const string PhoneNumberError = "Invalid phone number format.";

    /// <summary>
    /// A player's DNI or document number: digits only, 6 to 15 of them.
    /// </summary>
    public const string DocumentNumber = @"^\d{6,15}$";

    /// <summary>
    /// Human-facing message for a document number that fails the pattern.
    /// </summary>
    public const string DocumentNumberError = "El documento debe contener solo números.";
}
