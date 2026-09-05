using System;

namespace Application.DTOs.Tournament.Request;

/// <summary>
/// Payload for enrolling a single team into a tournament's registration phase.
/// </summary>
public class EnrollTeamRequest
{
    /// <summary>
    /// The existing team to enroll; mutually exclusive with NewTeamName.
    /// </summary>
    public Guid? ExistingTeamId { get; set; }

    /// <summary>
    /// The name of a brand-new team to create and enroll; mutually exclusive with ExistingTeamId.
    /// </summary>
    public string? NewTeamName { get; set; }

    /// <summary>
    /// Optional source season whose roster is copied in; only allowed together with ExistingTeamId.
    /// </summary>
    public Guid? CopyRosterFromTournamentId { get; set; }
}
