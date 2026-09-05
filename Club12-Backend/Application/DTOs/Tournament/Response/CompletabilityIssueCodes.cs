namespace Application.DTOs.Tournament.Response;

/// <summary>
/// The stable, English and neutral codes for every completability violation, localized by the frontend.
/// </summary>
public static class CompletabilityIssueCodes
{
    /// <summary>
    /// A regular zone has fewer than the required assigned teams.
    /// </summary>
    public const string ZoneTooFewTeams = nameof(ZoneTooFewTeams);

    /// <summary>
    /// An enrolled team is not assigned to any regular zone.
    /// </summary>
    public const string TeamNotAssigned = nameof(TeamNotAssigned);

    /// <summary>
    /// A team is assigned to more than one regular zone.
    /// </summary>
    public const string TeamInMultipleZones = nameof(TeamInMultipleZones);

    /// <summary>
    /// A playoff range starts beyond the zone's assigned team count.
    /// </summary>
    public const string PlayoffRangeExceedsTeams = nameof(PlayoffRangeExceedsTeams);

    /// <summary>
    /// A cross-division-cup group has fewer than the required assigned teams.
    /// </summary>
    public const string CrossCupGroupTooFewTeams = nameof(CrossCupGroupTooFewTeams);

    /// <summary>
    /// An enrolled team has fewer than the required registered players.
    /// </summary>
    public const string TeamTooFewPlayers = nameof(TeamTooFewPlayers);
}
