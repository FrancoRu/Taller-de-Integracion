namespace Application.Utils.Options;

/// <summary>
/// Configurable roster limits (HU-54). Bound from the "Roster" configuration
/// section; when the section is absent the defaults below apply.
/// </summary>
public class RosterOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "Roster";

    /// <summary>
    /// Maximum number of players allowed in a single team's roster for one
    /// tournament (season). Registrations beyond this are rejected (HU-54).
    /// </summary>
    public int MaxPlayersPerTeam { get; set; } = 25;
}
