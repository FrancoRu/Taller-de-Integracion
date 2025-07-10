using Entities.DTOs.Abstract;

namespace Entities.DTOs.Divisions;

/// <summary>
/// Represents a minimal response for a tournament.
/// </summary>
public class MinimalDivisionResponse : BaseEntityResponse
{
    /// <summary>
    /// The name of the division.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// If the division is finished.
    /// </summary>
    public required bool IsFinished { get; set; }
}
