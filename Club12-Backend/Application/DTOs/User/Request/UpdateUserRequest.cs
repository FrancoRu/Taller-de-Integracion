using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.User.Request;

public sealed class UpdateUserRequest
{
    [StringLength(50, MinimumLength = 3)]
    public string? Username { get; set; }

    [EmailAddress]
    public string? Email { get; set; }

    [Phone]
    public string? Phone { get; set; }
}