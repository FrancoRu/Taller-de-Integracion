using Entities.Models.StaffEnum;
using Entities.Models.TeamEntity;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.Models.StaffEntity;

/// <summary>
/// Represents a staff member in the Club12 application.
/// </summary>
[Table("Staffs", Schema = "Club12")]
public class Staff : EntityBase
{
    /// <summary>
    /// The names of the staff member.
    /// </summary>
    [Required]
    [MaxLength(50)]
    public required string Names { get; set; }

    /// <summary>
    /// The last name of the staff member.
    /// </summary>
    [Required]
    [MaxLength(50)]
    public required string LastName { get; set; }

    /// <summary>
    /// The phone number of the staff member.
    /// </summary>
    [Required]
    [MaxLength(15)]
    public required string PhoneNumber { get; set; }

    /// <summary>
    /// The type of staff member (COACH, DELEGATE, or SUB DELEGATE).
    /// </summary>
    [Required]
    public StaffType Type { get; set; }

    /// <summary>
    /// The team the staff belongs to.
    /// </summary>
    [Column(nameof(TeamId))]
    [Required]
    public required Team Team { get; set; }

    /// <summary>
    /// The Id of the team the staff belongs to.
    /// </summary>
    public Guid TeamId { get; set; }
}
