using System.ComponentModel.DataAnnotations;

namespace Club12.Services.DTOs.Division;

/// <summary>
/// Represents a request to create a division.
/// </summary>
public record DivisionRequest
{
    /// <summary>
    /// The name of the division.
    /// </summary>
    [Required(ErrorMessage = "The Name field is required.")]
    public required string Name { get; set; }
}
