namespace Domain.Enums;

/// <summary>
/// The lifecycle state of a match's result, distinct from the boolean Match.IsFinished flag.
/// </summary>
public enum MatchStatus
{
    /// <summary>
    /// The default state: the fixture exists but no result has been loaded yet.
    /// </summary>
    Scheduled,

    /// <summary>
    /// The match was played and a decisive result was loaded.
    /// </summary>
    Played,

    /// <summary>
    /// The match was postponed or suspended and has no result.
    /// </summary>
    Suspended,

    /// <summary>
    /// One team did not show up, so the regulation default result was applied to the present team.
    /// </summary>
    WalkOver,

    /// <summary>
    /// The match will never be played because its tournament was canceled or force-closed while it was still pending.
    /// </summary>
    Canceled
}
