using System;
namespace Application.DTOs.Scorer.Response;

/// <summary>
/// Response model representing a scorer's performance in a match.
/// </summary>
public class ScorerByTeamResponse : ScorerBaseResponse
{
    /// <summary>
    /// Unique identifier of the team.
    /// </summary>
    public required Guid TeamId { get; set; }

    /// <summary>
    /// Full name of the team.
    /// </summary>
    public required string Name { get; set; }

}
