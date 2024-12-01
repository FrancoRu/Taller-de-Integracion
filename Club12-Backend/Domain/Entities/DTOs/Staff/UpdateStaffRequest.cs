using Entities.Models.StaffEnum;

using System.ComponentModel.DataAnnotations;

namespace Entities.DTOs.Staff;

/// <summary>
/// Represents the staff member update request.
/// </summary>
public class UpdateStaffRequest
{
    /// <summary>
    /// The first name of the staff member.
    /// </summary>
    [MaxLength(50)]
    public string? Names { get; set; }

    /// <summary>
    /// The last name of the staff member.
    /// </summary>
    [MaxLength(50)]
    public string? LastName { get; set; }

    /// <summary>
    /// The phone number of the staff member.
    /// </summary>
    [MaxLength(15)]
    public string? PhoneNumber { get; set; }

    /// <summary>
    /// The type of the staff member (e.g., COACH, DELEGATE, SUB DELEGATE).
    /// </summary>
    public StaffType? Type { get; set; }
}
