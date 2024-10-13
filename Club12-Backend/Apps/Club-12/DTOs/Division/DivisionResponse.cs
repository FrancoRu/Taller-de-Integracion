using Club12.DTOs.Abstract;

namespace Club12.DTOs.Division;

/// <summary>
/// Represents a response for a division, inheriting from the base response.
/// </summary>
public record DivisionResponse : GenericEntity
{
    /// <summary>
    /// The name of the division.
    /// </summary>
    public required string Name { get; set; }
}
