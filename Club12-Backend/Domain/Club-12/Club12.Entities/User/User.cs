using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Club12.Entities.UserEntity;

[Table("Users", Schema = "Club12")]
public class User : EntityBase
{
    [Required]
    [MaxLength(25)]
    public required string UserName { get; set; }

    [Required]
    [MaxLength(64)]
    public required string PasswordHashed { get; set; }

    [Required]
    [MaxLength(10)]
    public required string Role { get; set; }
}
