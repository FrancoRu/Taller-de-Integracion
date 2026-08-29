namespace Application.Utils.Constants.Validation;

/// <summary>
/// Field length limits shared across the account/user request DTOs
/// (RegisterUserRequest, UpdateUserRequest, ChangePasswordRequest,
/// PasswordResetConfirmRequest), so they stay in sync.
/// </summary>
public static class UserFieldLengths
{
    public const int UsernameMinLength = 3;
    public const int UsernameMaxLength = 50;
    public const int PasswordMinLength = 8;
    public const int PhoneMaxLength = 15;
}
