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

    [Phone]
    public string? Phone { get; set; }

    /// <summary>
    /// Optional. When provided, changes the target user's role, replacing
    /// whatever role they currently have (a user always ends up in exactly
    /// one role). Leave null to update profile fields without touching the
    /// role. Only ADMIN and OWNER may set this, and the exact roles each
    /// may assign — plus the guard against changing your own role — are
    /// enforced server-side in the identity user-management service.
    /// </summary>
    public UserRoleType? Role { get; set; }
}