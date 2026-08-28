using System;
using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Roster.Request;

/// <summary>
/// Request to clone a roster from a previous season's team into a new season's
/// team (HU-53). The target team is taken from the route; this body identifies
/// the source team + season to copy from and the target season to copy into.
/// </summary>
public class CopyRosterRequest
{
    /// <summary>The past-season team whose roster is the source.</summary>
    [Required]
    public required Guid SourceTeamId { get; set; }

    /// <summary>The season (tournament) the source roster belongs to.</summary>
    [Required]
    public required Guid SourceTournamentId { get; set; }

    /// <summary>The new season (tournament) the roster is cloned into.</summary>
    [Required]
    public required Guid TargetTournamentId { get; set; }
}
