using Entities.Models.StaffEnum;

using System.ComponentModel.DataAnnotations;

namespace Entities.DTOs.Staff;

/// <summary>
/// Represents the staff member creation request.
/// </summary>
public class CreateStaffRequest
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
    /// The type of staff member (e.g., COACH, DELEGATE, SUB DELEGATE).
    /// </summary>
    [Required]
    [AllowedValues(StaffType.Delegate, StaffType.Coach, StaffType.SubDelegate)]
    public required StaffType Type { get; set; }

    /// <summary>
    /// The ID of the team the staff belongs to.
    /// </summary>
    [Required]
    public required Guid TeamId { get; set; }
}
