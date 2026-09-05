using System;
using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.MatchSeries.Request;

/// <summary>
/// Payload for creating a new best-of-N playoff series between two teams at one bracket round.
/// </summary>
public class CreateMatchSeriesRequest
{
    /// <summary>
    /// The stage this series belongs to; its BestOf value is copied onto the series at creation time.
    /// </summary>
    [Required(ErrorMessage = "Stage ID field is required.")]
    public required Guid StageId { get; set; }

    [Required(ErrorMessage = "Home team ID field is required.")]
    public required Guid HomeTeamId { get; set; }

    [Required(ErrorMessage = "Visitor team ID field is required.")]
    public required Guid VisitorTeamId { get; set; }
}
