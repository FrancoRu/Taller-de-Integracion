using Entities.Models.TeamEntity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.Models.StaffEntity;

/// <summary>
/// Represents a staff member with personal and role-related information.
/// </summary>
[Table("Staffs", Schema = "Club12")]
public class Staff: EntityBase
{
    /// <summary>
    /// The first name of the staff member.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// The last name of the staff member.
    /// </summary>
    public required string LastName { get; set; }

    /// <summary>
    /// The phone number of the staff member.
    /// Optional field.
    /// </summary>
    public string? PhoneNumber { get; set; }

    /// <summary>
    /// The type of staff role, such as delegate or subdelegate.
    /// </summary>
    public required StaffType StaffType { get; set; }

    /// <summary>
    /// The team the player belongs to.
    /// </summary>
    [Column(nameof(TeamId))]
    [Required]
    public required Team Team { get; set; }

    /// <summary>
    /// The Id of the team the player belongs to.
    /// </summary>
    public Guid TeamId { get; set; }
}