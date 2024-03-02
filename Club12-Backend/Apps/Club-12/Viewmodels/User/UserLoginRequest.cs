using System.ComponentModel.DataAnnotations;

namespace Club12.Viewmodels.User;

public class UserLoginRequest
{
    [Required]
    public required string UserName { get; set; }

    [Required]
    public required string Password { get; set; }
}
