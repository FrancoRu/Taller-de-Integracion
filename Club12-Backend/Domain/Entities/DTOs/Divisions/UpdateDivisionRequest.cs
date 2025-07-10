using System.ComponentModel.DataAnnotations;

namespace Entities.DTOs.Divisions;

/// <summary>
/// Represents a request to create a division.
/// </summary>
public class UpdateDivisionRequest
{
    /// <summary>
    /// The name of the division.
    /// </summary>
    [Required(ErrorMessage = "The Name field is required.")]
    public required string Name { get; set; }
}
