using Entities.DTOs.Abstract;
using Entities.DTOs.Match;
using Entities.DTOs.Team;

namespace Entities.DTOs.Division;

/// <summary>
/// Represents a response for a division, inheriting from the base response.
/// </summary>
public class DivisionResponse : BaseEntityResponse
{
    /// <summary>
    /// The name of the division.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// If the division is finished.
    /// </summary>
    public required bool IsFinished { get; set; }

    /// <summary>
    /// The teams in the division.
    /// </summary>
    public required IEnumerable<TeamResponse> Teams { get; set; }

    /// <summary>
    /// The Id of the tournament this division belongs to.
    /// </summary>
    public required Guid TournamentId { get; set; }

    /// <summary>
    /// The matches in the division.
    /// </summary>
    public required IEnumerable<MatchResponse> Matches { get; set; }
}
