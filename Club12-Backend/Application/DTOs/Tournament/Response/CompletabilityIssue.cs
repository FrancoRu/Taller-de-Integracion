namespace Application.DTOs.Tournament.Response;

/// <summary>
/// The stable, English/neutral codes for every completability violation
/// (HU-109). The frontend renders its own Spanish copy from these codes, so the
/// backend never emits localized text — only the code plus the structured
/// fields needed to build a message.
/// </summary>
public static class CompletabilityIssueCodes
{
    /// <summary>A regular zone has fewer than the required assigned teams.</summary>
    public const string ZoneTooFewTeams = nameof(ZoneTooFewTeams);

    /// <summary>An enrolled team is not assigned to any regular zone.</summary>
    public const string TeamNotAssigned = nameof(TeamNotAssigned);

    /// <summary>A team is assigned to more than one regular zone.</summary>
    public const string TeamInMultipleZones = nameof(TeamInMultipleZones);

    /// <summary>A playoff range starts beyond the zone's assigned team count.</summary>
    public const string PlayoffRangeExceedsTeams = nameof(PlayoffRangeExceedsTeams);

    /// <summary>A cross-division-cup group has fewer than the required assigned teams.</summary>
    public const string CrossCupGroupTooFewTeams = nameof(CrossCupGroupTooFewTeams);
}

/// <summary>
/// One completability violation found while checking whether a tournament can be
/// started (HU-109). Every field except <see cref="Code"/> is optional and only
/// populated for the rules that carry it, so the same shape covers every rule.
/// Text is intentionally English/neutral — the frontend localizes from
/// <see cref="Code"/>.
/// </summary>
public class CompletabilityIssue
{
    /// <summary>
    /// The violated rule's stable code — one of
    /// <see cref="CompletabilityIssueCodes"/>.
    /// </summary>
    public required string Code { get; set; }

    /// <summary>
    /// The offending division/zone name, when the rule is about a division
    /// (ZoneTooFewTeams, PlayoffRangeExceedsTeams, CrossCupGroupTooFewTeams).
    /// </summary>
    public string? DivisionName { get; set; }

    /// <summary>
    /// The offending team name, when the rule is about a team
    /// (TeamNotAssigned, TeamInMultipleZones).
    /// </summary>
    public string? TeamName { get; set; }

    /// <summary>
    /// The playoff range's first position, for PlayoffRangeExceedsTeams.
    /// </summary>
    public int? FromPosition { get; set; }

    /// <summary>
    /// The number of teams assigned to the offending division/group, for the
    /// count-based rules (ZoneTooFewTeams, PlayoffRangeExceedsTeams,
    /// CrossCupGroupTooFewTeams).
    /// </summary>
    public int? AssignedTeams { get; set; }
}
