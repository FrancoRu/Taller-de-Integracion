using Application.Utils.Constants.Validation;

using Domain.Enums;

using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.User.Request;

public sealed class UpdateUserRequest
{
    [StringLength(UserFieldLengths.UsernameMaxLength, MinimumLength = UserFieldLengths.UsernameMinLength)]
    public string? Username { get; set; }

    [EmailAddress]
    public string? Email { get; set; }

    [ArgentinePhoneNumber]
    [MaxLength(UserFieldLengths.PhoneMaxLength, ErrorMessage = "Phone number must not exceed 15 characters.")]
    public string? Phone { get; set; }

    /// <summary>
    /// When provided, replaces the target user's role; null updates profile fields without touching it.
    /// </summary>
    public UserRoleType? Role { get; set; }
}