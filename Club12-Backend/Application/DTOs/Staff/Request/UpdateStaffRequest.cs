using Application.Utils.Constants.Validation;

using Domain.Enums;

using System.ComponentModel.DataAnnotations;
namespace Application.DTOs.Staff.Request;

/// <summary>
/// Represents the staff member update request.
/// </summary>
public class UpdateStaffRequest
{
    /// <summary>
    /// The first name of the staff member.
    /// </summary>
    [MaxLength(StaffFieldLengths.NameMaxLength)]
    public string? Names { get; set; }

    /// <summary>
    /// The last name of the staff member.
    /// </summary>
    [MaxLength(StaffFieldLengths.NameMaxLength)]
    public string? LastName { get; set; }

    /// <summary>
    /// The phone number of the staff member.
    /// </summary>
    [MaxLength(StaffFieldLengths.PhoneNumberMaxLength)]
    public string? PhoneNumber { get; set; }

    /// <summary>
    /// The type of the staff member (e.g., COACH, DELEGATE, SUB DELEGATE).
    /// </summary>
    public StaffType? Type { get; set; }
}
