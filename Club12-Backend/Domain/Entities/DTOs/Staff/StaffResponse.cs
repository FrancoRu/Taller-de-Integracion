using Entities.DTOs.Abstract;

namespace Entities.DTOs.Staff;

/// <summary>
/// Represents the staff member response for API calls.
/// </summary>
public class StaffResponse : BaseEntityResponse
{
    /// <summary>
    /// The names of the staff member.
    /// </summary>
    public required string Names { get; set; }

    /// <summary>
    /// The last name of the staff member.
    /// </summary>
    public required string LastName { get; set; }

    /// <summary>
    /// The phone number of the staff member.
    /// </summary>
    public required string PhoneNumber { get; set; }

    /// <summary>
    /// The type of the staff member (e.g., COACH, DELEGATE, SUB DELEGATE).
    /// </summary>
    public required string Type { get; set; }

    /// <summary>
    /// The id of the team the staff belongs to.
    /// </summary>
    public required Guid TeamId { get; set; }
}
