using System;

namespace Application.DTOs.Tournament.Request;

/// <summary>
/// HU-107: payload for enrolling a single team into a tournament's registration
/// phase. Exactly one of <see cref="ExistingTeamId"/> or
/// <see cref="NewTeamName"/> must be provided:
/// <list type="bullet">
/// <item><see cref="NewTeamName"/> creates a brand-new team with an empty roster.</item>
/// <item><see cref="ExistingTeamId"/> enrolls a team that already exists (a club
/// from another season) — the same Team identity is reused (HU-99), a new
/// season registration is created.</item>
/// </list>
/// <see cref="CopyRosterFromTournamentId"/> is optional and only valid together
/// with <see cref="ExistingTeamId"/>: it clones that team's roster from the
/// given season into this tournament as an editable base (HU-53). Medical
/// records are never inherited (HU-59).
/// </summary>
public class EnrollTeamRequest
{
    /// <summary>
    /// The existing team to enroll. Mutually exclusive with
    /// <see cref="NewTeamName"/>.
    /// </summary>
    public Guid? ExistingTeamId { get; set; }

    /// <summary>
    /// The name of a brand-new team to create and enroll. Mutually exclusive
    /// with <see cref="ExistingTeamId"/>.
    /// </summary>
    public string? NewTeamName { get; set; }

    /// <summary>
    /// Optional source season whose roster is copied into this tournament as an
    /// editable base. Only allowed together with <see cref="ExistingTeamId"/>.
    /// </summary>
    public Guid? CopyRosterFromTournamentId { get; set; }
}
