using Entities.DTOs.Stage;

namespace Entities.DTOs.Divisions;

/// <summary>
/// Represents the response structure for a division, 
/// including details about the division, its matches, and positions.
/// </summary>
public class DivisionResponse
{
    /// <summary>
    /// Gets or sets the unique identifier of the division.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the name of the division.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether the division has finished.
    /// </summary>
    public bool IsFinished { get; set; }

    /// <summary>
    /// Gets or sets the list of positions for teams in the division.
    /// </summary>
    public List<PositionResponse>? Positions { get; set; }

    /// <summary>
    /// Gets or sets the ID of the tournament to which the division belongs.
    /// </summary>
    public Guid TournamentId { get; set; }

    /// <summary>
    /// Gets or sets the list of matches for the division, grouped by stage or week.
    /// </summary>
    public List<StageResponse>? Stages { get; set; }
}
