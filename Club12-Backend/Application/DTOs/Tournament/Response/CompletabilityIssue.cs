namespace Application.DTOs.Tournament.Response;

/// <summary>
/// One completability violation, with every field except Code optional and populated only where relevant.
/// </summary>
public class CompletabilityIssue
{
    /// <summary>
    /// The violated rule's stable code, one of the constants in CompletabilityIssueCodes.
    /// </summary>
    public required string Code { get; set; }

    /// <summary>
    /// The offending division or zone name, when the rule is about a division.
    /// </summary>
    public string? DivisionName { get; set; }

    /// <summary>
    /// The offending team name, when the rule is about a team.
    /// </summary>
    public string? TeamName { get; set; }

    /// <summary>
    /// The playoff range's first position, for PlayoffRangeExceedsTeams.
    /// </summary>
    public int? FromPosition { get; set; }

    /// <summary>
    /// The number of teams assigned to the offending division or group, for the count-based rules.
    /// </summary>
    public int? AssignedTeams { get; set; }

    /// <summary>
    /// The offending team's habilitado player count, not its raw roster size, for TeamTooFewPlayers.
    /// </summary>
    public int? PlayerCount { get; set; }
}
