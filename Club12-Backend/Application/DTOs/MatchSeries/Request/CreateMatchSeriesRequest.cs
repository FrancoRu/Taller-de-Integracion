using System;
using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.MatchSeries.Request;

/// <summary>
/// Represents the payload for creating a new best-of-N playoff series
/// between two teams at one bracket round.
/// </summary>
public class CreateMatchSeriesRequest
{
    /// <summary>
    /// The stage (round) this series belongs to. Its BestOf value is
    /// copied onto the series at creation time.
    /// </summary>
    [Required(ErrorMessage = "Stage ID field is required.")]
    public required Guid StageId { get; set; }

    /// <summary>
    /// The home team in the series.
    /// </summary>
    [Required(ErrorMessage = "Home team ID field is required.")]
    public required Guid HomeTeamId { get; set; }

    /// <summary>
    /// The visitor team in the series.
    /// </summary>
    [Required(ErrorMessage = "Visitor team ID field is required.")]
    public required Guid VisitorTeamId { get; set; }
}
