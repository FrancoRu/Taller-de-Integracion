namespace Application.Utils.Constants.Validation;

/// <summary>
/// Field length limits shared by CreateStaffRequest and
/// UpdateStaffRequest, so the two stay in sync.
/// </summary>
public static class StaffFieldLengths
{
    public const int NameMaxLength = 50;
    public const int PhoneNumberMaxLength = 15;
}
