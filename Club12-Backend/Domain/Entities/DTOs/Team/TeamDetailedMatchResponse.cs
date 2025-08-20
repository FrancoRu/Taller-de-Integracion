using Entities.DTOs.Player;
using Entities.DTOs.Scorer;

namespace Entities.DTOs.Team;

/// <summary>
/// Represents the response data for a team in a match.
/// </summary>
public class TeamDetailedMatchResponse
{
    /// <summary>
    /// The unique identifier of the team.
    /// </summary>
    public required Guid Id { get; set; }

    /// <summary>
    /// The name of the team.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// The URL of the team's logo.
    /// </summary>
    public required string LogoUrl { get; set; }

    /// <summary>
    /// The score of the team.
    /// </summary>
    public int Score { get; set; }

    /// <summary>
    /// List of scorers for the team.
    /// </summary>
    public List<ScorerResponse> Scorers { get; set; } = [];

    public List<PublicPlayerResponse> Players { get; set; } = [];
}
