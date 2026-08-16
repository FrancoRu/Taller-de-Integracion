using Application.Utils.Constants.Validation;

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
}