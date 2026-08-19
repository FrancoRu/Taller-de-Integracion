namespace Application.Utils.Constants.Validation;

/// <summary>
/// Field length limits shared across the player sanction request DTOs
/// (Create/Update/Appeal/Resolve), so they stay in sync.
/// </summary>
public static class SanctionFieldLengths
{
    /// <summary>
    /// Max length of the short sanction description (Create/UpdatePlayerSanctionRequest).
    /// </summary>
    public const int DescriptionMaxLength = 255;

    /// <summary>
    /// Min length of the free-text appeal reason / resolution notes.
    /// </summary>
    public const int LongTextMinLength = 1;

    /// <summary>
    /// Max length of the free-text appeal reason / resolution notes.
    /// </summary>
    public const int LongTextMaxLength = 1000;
}
