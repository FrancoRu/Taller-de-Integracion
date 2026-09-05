using System;
namespace Application.DTOs.Scorer.Response;

/// <summary>
/// Response model representing a scorer's performance in a match.
/// </summary>
public class ScorerByTeamResponse : ScorerBaseResponse
{
    public required Guid TeamId { get; set; }

    public required string Name { get; set; }

}
