namespace Domain.Enums;

/// <summary>
/// The lifecycle state of a match's result (HU-69). Distinct from
/// <see cref="Domain.Entities.Models.Match.IsFinished"/>, which stays as the
/// boolean "has a final result" flag: a match is finished when its status is
/// <see cref="Played"/> or <see cref="WalkOver"/>.
/// </summary>
public enum MatchStatus
{
    /// <summary>
    /// The fixture exists but no result has been loaded yet (default).
    /// </summary>
    Scheduled,

    /// <summary>
    /// The match was played and a decisive result was loaded (HU-69/HU-70).
    /// </summary>
    Played,

    /// <summary>
    /// The match was postponed/suspended and has no result (HU-68/HU-73).
    /// </summary>
    Suspended,

    /// <summary>
    /// One team did not show up; the regulation default result was applied to
    /// the present team (HU-73). Distinguishable from a normal <see cref="Played"/>
    /// result so the UI can flag it.
    /// </summary>
    WalkOver,

    /// <summary>
    /// The match will never be played: its tournament was moved to
    /// <see cref="Domain.Enums.TournamentStatus.Canceled"/> or force-closed to
    /// <see cref="Domain.Enums.TournamentStatus.Finished"/> while the match was
    /// still pending. Distinct from <see cref="Suspended"/> (a real match
    /// postponed for later) — a canceled match is permanently dead.
    /// </summary>
    Canceled
}
