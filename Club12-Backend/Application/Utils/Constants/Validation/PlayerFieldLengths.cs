namespace Application.Utils.Constants.Validation;

/// <summary>
/// Field length limits shared by CreatePlayerRequest and UpdatePlayerRequest, so the two stay in sync.
/// </summary>
public static class PlayerFieldLengths
{
    public const int NameMaxLength = 70;
    public const int DocumentNumberMaxLength = 15;
    public const int PhoneNumberMaxLength = 15;
    public const int SocialSecurityMaxLength = 100;
}
