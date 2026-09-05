using Application.Utils.Constants.Stage;

namespace Application.DTOs.Stage.Request;

/// <summary>
/// Represents the payload for updating an existing stage.
/// </summary>
public class UpdateStageRequest
{
    public string? Description { get; set; }

    /// <summary>
    /// Indicates whether the stage is currently active.
    /// If null, the default should be true.
    /// </summary>
    public bool? IsActive { get; set; }

    /// <summary>
    /// Optional label grouping this stage with other parallel elimination
    /// brackets in the same division. Null clears the bracket grouping.
    /// </summary>
    public string? BracketName { get; set; }

    /// <summary>
    /// Number of games in a series between two teams at this round: 1, 3,
    /// 5, or 7. Null leaves the existing value unchanged.
    /// </summary>
    [System.ComponentModel.DataAnnotations.AllowedValues(1, 3, 5, 7)]
    public int? BestOf { get; set; }

    /// <summary>
    /// How many times each pair of teams plays within this group stage.
    /// Null leaves the existing value unchanged.
    /// </summary>
    [System.ComponentModel.DataAnnotations.Range(RoundRobinFormat.MIN_LEGS, RoundRobinFormat.MAX_LEGS)]
    public int? RoundRobinLegs { get; set; }
}
